using System.Net;
using System.Text;
using Fieldore.Application.Estimates.Contracts;
using Fieldore.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Fieldore.API.Controllers;

// Server-rendered, client-facing quote and invoice pages (no JWT required).
[AllowAnonymous]
public sealed class PublicPagesController(IEstimateService estimateService, FieldoreDbContext dbContext) : ControllerBase
{
    [HttpGet("/quote/{token:guid}")]
    public async Task<IActionResult> ViewQuote(Guid token, CancellationToken cancellationToken)
    {
        var result = await estimateService.GetPublicByTokenAsync(token, cancellationToken);
        if (!result.Success || result.Data is null)
        {
            return Html(BuildResultPage("Quote not found", "This quote link is invalid or has been removed.", false), 404);
        }

        return Html(BuildQuotePage(result.Data, token));
    }

    [HttpPost("/quote/{token:guid}/accept")]
    public async Task<IActionResult> AcceptQuote(Guid token, CancellationToken cancellationToken)
    {
        var result = await estimateService.RespondPublicAsync(token, true, cancellationToken);
        return Html(result.Success
            ? BuildResultPage("Quote accepted ✓", "Thank you! We've let the provider know you've accepted this quote. They'll be in touch to schedule the work.", true)
            : BuildResultPage("Unable to accept", result.Message ?? "Something went wrong.", false),
            result.Success ? 200 : result.StatusCode);
    }

    [HttpPost("/quote/{token:guid}/reject")]
    public async Task<IActionResult> RejectQuote(Guid token, CancellationToken cancellationToken)
    {
        var result = await estimateService.RespondPublicAsync(token, false, cancellationToken);
        return Html(result.Success
            ? BuildResultPage("Quote declined", "Thanks for letting us know. We've recorded that you've declined this quote.", true)
            : BuildResultPage("Unable to decline", result.Message ?? "Something went wrong.", false),
            result.Success ? 200 : result.StatusCode);
    }

    private ContentResult Html(string html, int statusCode = 200) =>
        new() { Content = html, ContentType = "text/html; charset=utf-8", StatusCode = statusCode };

    private static string BuildQuotePage(PublicEstimateResponse quote, Guid token)
    {
        var rows = new StringBuilder();
        foreach (var item in quote.LineItems)
        {
            rows.Append("<tr><td>")
                .Append(Enc(item.ServiceName))
                .Append(string.IsNullOrWhiteSpace(item.Description) ? "" : $"<div class='muted'>{Enc(item.Description!)}</div>")
                .Append("</td><td class='num'>")
                .Append(item.Quantity.ToString("0.##"))
                .Append("</td><td class='num'>")
                .Append(Money(item.UnitPrice, quote.Currency))
                .Append("</td><td class='num'>")
                .Append(Money(item.LineTotal, quote.Currency))
                .Append("</td></tr>");
        }

        var actions = quote.CanRespond
            ? $@"<div class='actions'>
                    <form method='post' action='/quote/{token}/accept'><button class='btn accept' type='submit'>Accept quote</button></form>
                    <form method='post' action='/quote/{token}/reject'><button class='btn reject' type='submit'>Decline</button></form>
                 </div>"
            : $"<div class='status-banner {StatusClass(quote.Status)}'>{StatusLabel(quote.Status)}</div>";

        var expiry = quote.ExpiresOn.HasValue ? $"<span class='muted'>Valid until {quote.ExpiresOn:dd MMM yyyy}</span>" : "";

        return Page($@"
            <div class='card'>
              <div class='head'>
                <div>
                  <div class='biz'>{Enc(quote.BusinessName)}</div>
                  <div class='muted'>Quote {Enc(quote.EstimateNumber)} · {quote.IssuedOn:dd MMM yyyy}</div>
                </div>
                <div class='total-badge'>{Money(quote.TotalAmount, quote.Currency)}</div>
              </div>
              <div class='muted to'>Prepared for {Enc(quote.CustomerNameSnapshot)}</div>
              <table>
                <thead><tr><th>Item</th><th class='num'>Qty</th><th class='num'>Rate</th><th class='num'>Amount</th></tr></thead>
                <tbody>{rows}</tbody>
              </table>
              <div class='totals'>
                <div><span>Subtotal</span><span>{Money(quote.SubtotalAmount, quote.Currency)}</span></div>
                {(quote.DiscountAmount > 0 ? $"<div><span>Discount</span><span>-{Money(quote.DiscountAmount, quote.Currency)}</span></div>" : "")}
                <div><span>Tax ({quote.TaxRate.ToString("0.##")}%)</span><span>{Money(quote.TaxAmount, quote.Currency)}</span></div>
                <div class='grand'><span>Total</span><span>{Money(quote.TotalAmount, quote.Currency)}</span></div>
              </div>
              {(string.IsNullOrWhiteSpace(quote.Notes) ? "" : $"<div class='notes'><div class='muted'>Notes</div>{Enc(quote.Notes!)}</div>")}
              {actions}
              <div class='foot'>{expiry}</div>
            </div>");
    }

    // ── Public Invoice Page ───────────────────────────────────────────────────

    [HttpGet("/invoice/{token:guid}")]
    public async Task<IActionResult> ViewInvoice(
        Guid token, [FromQuery] bool paid = false, CancellationToken cancellationToken = default)
    {
        var invoice = await dbContext.Invoices
            .AsNoTracking()
            .Include(x => x.LineItems)
            .Include(x => x.Payments)
            .FirstOrDefaultAsync(x => x.PublicToken == token, cancellationToken);

        if (invoice is null)
            return Html(BuildResultPage("Invoice not found", "This invoice link is invalid or has been removed.", false), 404);

        var business = await dbContext.Businesses
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == invoice.BusinessId, cancellationToken);

        var hasStripe = business?.StripeOnboardingComplete == true
                        && !string.IsNullOrEmpty(business.StripeAccountId);

        // Authoritative paid state comes from the invoice itself, never the transient query flag —
        // otherwise reopening the link later (without ?paid=1) would show Pay Now again.
        var isPaid = invoice.Status == "paid" || invoice.BalanceDueAmount <= 0;

        var successBanner = paid
            ? "<div class='success-banner'>✓ Payment received — thank you!</div>"
            : "";

        return Html(BuildInvoicePage(invoice, business, hasStripe && !isPaid, token, successBanner));
    }

    private static string BuildInvoicePage(
        Fieldore.Domain.Entities.Invoice invoice, Fieldore.Domain.Entities.Business? business,
        bool showPayBtn, Guid token, string banner)
    {
        var currency = business?.Currency ?? "USD";

        var lineRows = new StringBuilder();
        foreach (var item in invoice.LineItems.OrderBy(x => x.SortOrder))
        {
            lineRows.Append("<tr><td>").Append(Enc(item.Name));
            if (!string.IsNullOrWhiteSpace(item.Description))
                lineRows.Append($"<div class='muted'>{Enc(item.Description!)}</div>");
            lineRows.Append("</td><td class='num'>").Append(item.Quantity.ToString("0.##"))
                    .Append("</td><td class='num'>").Append(Money(item.UnitRate, currency))
                    .Append("</td><td class='num'>").Append(Money(item.LineTotal, currency))
                    .Append("</td></tr>");
        }

        var paymentRows = new StringBuilder();
        foreach (var p in invoice.Payments.Where(x => !x.IsRefund).OrderByDescending(x => x.PaidAt))
        {
            paymentRows.Append($"<div class='pay-row'><span>{p.PaidAt:dd MMM yyyy} — {FormatMethod(p.Method)}</span><span class='green'>-{Money(p.Amount, currency)}</span></div>");
        }
        foreach (var r in invoice.Payments.Where(x => x.IsRefund).OrderByDescending(x => x.PaidAt))
        {
            paymentRows.Append($"<div class='pay-row'><span>{r.PaidAt:dd MMM yyyy} — Refund</span><span class='red'>+{Money(r.Amount, currency)}</span></div>");
        }

        var payBtn = showPayBtn && invoice.BalanceDueAmount > 0
            ? $"<form method='post' action='/api/stripe/invoice/{token}/checkout'><button class='btn pay' type='submit'>Pay {Money(invoice.BalanceDueAmount, currency)} Now</button></form>"
            : "";

        var statusBadge = invoice.Status switch
        {
            "paid"          => "<span class='badge green'>Paid</span>",
            "partially_paid"=> "<span class='badge amber'>Partially Paid</span>",
            "overdue"       => "<span class='badge red'>Overdue</span>",
            "void"          => "<span class='badge muted'>Void</span>",
            "viewed"        => "<span class='badge blueb'>Viewed</span>",
            "sent"          => "<span class='badge blueb'>Sent</span>",
            _               => "<span class='badge muted'>Draft</span>"
        };

        var businessName = business?.Name ?? "Your Business";
        var payeeAddress = FormatAddress(business?.Address);
        var payeeContact = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(business?.Email)) payeeContact.Append($"<div>{Enc(business.Email!)}</div>");
        if (!string.IsNullOrWhiteSpace(business?.Phone)) payeeContact.Append($"<div>{Enc(business.Phone!)}</div>");
        if (!string.IsNullOrWhiteSpace(business?.WebsiteUrl)) payeeContact.Append($"<div>{Enc(business.WebsiteUrl!)}</div>");

        var billToAddress = FormatAddress(invoice.BillingAddressSnapshot);
        var billToContact = string.IsNullOrWhiteSpace(invoice.CustomerEmailSnapshot)
            ? "" : $"<div>{Enc(invoice.CustomerEmailSnapshot!)}</div>";

        var logoOrInitial = string.IsNullOrWhiteSpace(business?.LogoUrl)
            ? $"<div class='logo-fallback'>{Enc(businessName.Length > 0 ? businessName[..1].ToUpperInvariant() : "F")}</div>"
            : $"<img class='logo-img' src='{Enc(business!.LogoUrl!)}' alt='{Enc(businessName)}' />";

        return Page($@"
            <div class='card invoice-card'>
              {banner}
              <div class='letterhead'>
                <div class='letterhead-left'>
                  {logoOrInitial}
                  <div>
                    <div class='biz'>{Enc(businessName)}</div>
                    {(payeeAddress.Length > 0 ? $"<div class='muted small'>{payeeAddress}</div>" : "")}
                    {payeeContact}
                  </div>
                </div>
                <div class='letterhead-right'>
                  <div class='inv-title'>INVOICE</div>
                  <div class='muted small'>{Enc(invoice.InvoiceNumber)}</div>
                  <div class='status-wrap'>{statusBadge}</div>
                </div>
              </div>

              <div class='meta-grid'>
                <div>
                  <div class='sec-title'>Bill To</div>
                  <div class='party-name'>{Enc(invoice.CustomerNameSnapshot)}</div>
                  {(billToAddress.Length > 0 ? $"<div class='muted small'>{billToAddress}</div>" : "")}
                  {billToContact}
                </div>
                <div class='meta-dates'>
                  <div><span class='muted small'>Issue Date</span><span>{invoice.IssuedOn:dd MMM yyyy}</span></div>
                  <div><span class='muted small'>Due Date</span><span>{invoice.DueOn:dd MMM yyyy}</span></div>
                  {(string.IsNullOrWhiteSpace(invoice.PurchaseOrderNumber) ? "" : $"<div><span class='muted small'>PO Number</span><span>{Enc(invoice.PurchaseOrderNumber!)}</span></div>")}
                  <div><span class='muted small'>Terms</span><span>{Enc(invoice.NetTerms)}</span></div>
                </div>
              </div>

              <table>
                <thead><tr><th>Item</th><th class='num'>Qty</th><th class='num'>Rate</th><th class='num'>Amount</th></tr></thead>
                <tbody>{lineRows}</tbody>
              </table>
              <div class='totals'>
                <div><span>Subtotal</span><span>{Money(invoice.SubtotalAmount, currency)}</span></div>
                {(invoice.DiscountAmount > 0 ? $"<div><span>Discount</span><span>-{Money(invoice.DiscountAmount, currency)}</span></div>" : "")}
                <div><span>Tax ({invoice.TaxRate.ToString("0.##")}%)</span><span>{Money(invoice.TaxAmount, currency)}</span></div>
                <div class='grand'><span>Total</span><span>{Money(invoice.TotalAmount, currency)}</span></div>
              </div>
              {(paymentRows.Length > 0 ? $"<div class='payments-section'><div class='sec-title'>Payment History</div>{paymentRows}</div>" : "")}
              {(invoice.BalanceDueAmount > 0 ? $"<div class='balance-row'><span class='bold'>Balance Due</span><span class='bold blue'>{Money(invoice.BalanceDueAmount, currency)}</span></div>" : "<div class='balance-row paid'><span class='bold'>Status</span><span class='bold green'>Paid in full ✓</span></div>")}
              {(string.IsNullOrWhiteSpace(invoice.Notes) ? "" : $"<div class='notes'><div class='muted small'>Notes</div>{Enc(invoice.Notes!)}</div>")}
              {payBtn}
              {(!showPayBtn && invoice.BalanceDueAmount > 0 ? "<p class='muted center'>Contact the business to arrange payment.</p>" : "")}
              <div class='powered'>Invoice generated via Fieldore</div>
            </div>");
    }

    private static string FormatAddress(Fieldore.Domain.ValueObjects.Address? address)
    {
        if (address is null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(address.Line1)) parts.Add(address.Line1!);
        if (!string.IsNullOrWhiteSpace(address.Line2)) parts.Add(address.Line2!);
        var cityLine = string.Join(", ", new[] { address.City, address.StateOrProvince, address.PostalCode }
            .Where(x => !string.IsNullOrWhiteSpace(x)));
        if (cityLine.Length > 0) parts.Add(cityLine);
        if (!string.IsNullOrWhiteSpace(address.Country)) parts.Add(address.Country!);
        return parts.Count == 0 ? "" : string.Join("<br/>", parts.Select(Enc));
    }

    private static string FormatMethod(string method) => method switch
    {
        "cash"          => "Cash",
        "card"          => "Card",
        "credit_card"   => "Credit Card",
        "debit_card"    => "Debit Card",
        "bank_transfer" => "Bank Transfer",
        "online"        => "Online",
        "check"         => "Check",
        "stripe"        => "Online (Stripe)",
        _               => method
    };

    // ── Shared helpers ────────────────────────────────────────────────────────

    private static string BuildResultPage(string title, string message, bool success) =>
        Page($@"<div class='card center'>
                  <div class='result-icon {(success ? "ok" : "err")}'>{(success ? "✓" : "!")}</div>
                  <h1>{Enc(title)}</h1>
                  <p class='muted'>{Enc(message)}</p>
                </div>");

    private static string Page(string body) => $@"<!doctype html>
<html lang='en'><head><meta charset='utf-8'>
<meta name='viewport' content='width=device-width, initial-scale=1'>
<title>Quote</title>
<style>
  :root{{--blue:#2563eb;--ink:#0f172a;--muted:#64748b;--line:#e2e8f0;--bg:#f1f5f9}}
  *{{box-sizing:border-box}} body{{margin:0;font-family:-apple-system,Segoe UI,Roboto,Helvetica,Arial,sans-serif;background:var(--bg);color:var(--ink);padding:24px}}
  .card{{max-width:560px;margin:24px auto;background:#fff;border:1px solid var(--line);border-radius:18px;padding:24px;box-shadow:0 8px 30px rgba(2,6,23,.06)}}
  .head{{display:flex;justify-content:space-between;align-items:flex-start;gap:12px}}
  .biz{{font-size:20px;font-weight:800}} .muted{{color:var(--muted);font-size:13px}}
  .total-badge{{background:#eff6ff;color:var(--blue);font-weight:800;padding:8px 12px;border-radius:12px;white-space:nowrap}}
  .to{{margin:6px 0 16px}}
  table{{width:100%;border-collapse:collapse;margin:8px 0}}
  th,td{{text-align:left;padding:10px 8px;border-bottom:1px solid var(--line);font-size:14px;vertical-align:top}}
  th{{font-size:11px;text-transform:uppercase;letter-spacing:.04em;color:var(--muted)}}
  .num{{text-align:right;white-space:nowrap}}
  .totals{{margin-top:12px}} .totals>div{{display:flex;justify-content:space-between;padding:6px 8px;font-size:14px}}
  .totals .grand{{font-weight:800;border-top:1px solid var(--line);margin-top:4px;padding-top:10px;font-size:16px}}
  .notes{{margin-top:16px;background:#f8fafc;border:1px solid var(--line);border-radius:12px;padding:12px;font-size:14px}}
  .actions{{display:flex;gap:12px;margin-top:22px}} .actions form{{flex:1;margin:0}}
  .btn{{width:100%;border:0;border-radius:14px;padding:14px;font-size:15px;font-weight:700;cursor:pointer}}
  .btn.accept{{background:var(--blue);color:#fff}} .btn.reject{{background:#fff;color:#dc2626;border:1px solid #fecaca}}
  .status-banner{{margin-top:22px;text-align:center;padding:14px;border-radius:14px;font-weight:700}}
  .status-banner.ok{{background:#ecfdf5;color:#059669}} .status-banner.no{{background:#fef2f2;color:#dc2626}} .status-banner.neutral{{background:#f8fafc;color:#475569}}
  .foot{{margin-top:16px;text-align:center}}
  .center{{text-align:center}} h1{{font-size:22px;margin:8px 0}}
  .result-icon{{width:64px;height:64px;border-radius:50%;display:flex;align-items:center;justify-content:center;font-size:32px;font-weight:800;margin:0 auto 8px}}
  .result-icon.ok{{background:#ecfdf5;color:#059669}} .result-icon.err{{background:#fef2f2;color:#dc2626}}
  .btn.pay{{background:var(--blue);color:#fff;margin-top:20px}} .btn.pay:hover{{background:#1d4ed8}}
  .payments-section{{margin-top:16px;border-top:1px solid var(--line);padding-top:12px}}
  .sec-title{{font-size:11px;font-weight:800;letter-spacing:.06em;color:var(--muted);margin-bottom:8px}}
  .pay-row{{display:flex;justify-content:space-between;font-size:13px;padding:4px 0;color:var(--ink)}}
  .balance-row{{display:flex;justify-content:space-between;border-top:2px solid var(--line);margin-top:12px;padding-top:12px;font-size:16px}}
  .badge{{font-size:10px;font-weight:800;padding:2px 8px;border-radius:8px;letter-spacing:.04em}}
  .badge.green{{background:#ecfdf5;color:#059669}} .badge.amber{{background:#fffbeb;color:#b45309}}
  .badge.red{{background:#fef2f2;color:#dc2626}} .badge.muted{{background:#f1f5f9;color:#64748b}}
  .success-banner{{background:#ecfdf5;color:#059669;font-weight:700;padding:12px 16px;border-radius:12px;margin-bottom:16px;text-align:center}}
  .green{{color:#059669}} .red{{color:#dc2626}} .blue{{color:var(--blue)}} .bold{{font-weight:800}}
  .invoice-card{{max-width:640px;padding:0;overflow:hidden}}
  .letterhead{{display:flex;justify-content:space-between;align-items:flex-start;gap:16px;padding:28px 28px 20px;background:linear-gradient(135deg,#0f172a,#1e293b);color:#fff}}
  .letterhead-left{{display:flex;align-items:flex-start;gap:14px}}
  .logo-img{{width:48px;height:48px;border-radius:12px;object-fit:cover;background:#fff}}
  .logo-fallback{{width:48px;height:48px;border-radius:12px;background:var(--blue);color:#fff;display:flex;align-items:center;justify-content:center;font-size:20px;font-weight:800;flex-shrink:0}}
  .letterhead .biz{{font-size:18px;font-weight:800;color:#fff}}
  .letterhead .muted.small{{color:#cbd5e1}}
  .letterhead-right{{text-align:right;flex-shrink:0}}
  .inv-title{{font-size:22px;font-weight:800;letter-spacing:.08em;color:#fff}}
  .status-wrap{{margin-top:8px}}
  .small{{font-size:12px}}
  .meta-grid{{display:flex;justify-content:space-between;gap:20px;padding:22px 28px 0;flex-wrap:wrap}}
  .party-name{{font-weight:700;font-size:14px;margin-bottom:2px}}
  .meta-dates{{display:flex;flex-direction:column;gap:6px;min-width:160px}}
  .meta-dates>div{{display:flex;justify-content:space-between;gap:16px;font-size:13px}}
  .meta-dates .muted.small{{flex-shrink:0}}
  .invoice-card table, .invoice-card .totals, .invoice-card .payments-section, .invoice-card .balance-row,
  .invoice-card .notes, .invoice-card .btn.pay, .invoice-card p.muted.center, .invoice-card .powered {{
    margin-left:28px;margin-right:28px;width:calc(100% - 56px);
  }}
  .invoice-card table{{margin-top:18px}}
  .balance-row.paid{{border-top:2px solid #d1fae5;background:#ecfdf5;border-radius:10px;padding:12px}}
  .badge.blueb{{background:#eff6ff;color:var(--blue)}}
  .powered{{margin-top:24px;margin-bottom:24px;text-align:center;color:#94a3b8;font-size:11px;letter-spacing:.04em}}
</style></head><body>{body}</body></html>";

    private static string StatusLabel(string status) => status switch
    {
        "approved" => "You have accepted this quote ✓",
        "converted" => "This quote has been accepted and scheduled ✓",
        "rejected" => "You have declined this quote",
        "expired" => "This quote has expired",
        _ => "This quote is no longer open for a response"
    };

    private static string StatusClass(string status) => status switch
    {
        "approved" or "converted" => "ok",
        "rejected" or "expired" => "no",
        _ => "neutral"
    };

    private static string Money(decimal amount, string currency)
    {
        var symbol = currency switch
        {
            "USD" => "$",
            "INR" => "₹",
            "EUR" => "€",
            "GBP" => "£",
            "CAD" => "$",
            "AUD" => "$",
            _ => ""
        };
        return string.IsNullOrEmpty(symbol)
            ? $"{currency} {amount:N2}"
            : $"{symbol}{amount:N2}";
    }

    private static string Enc(string value) => WebUtility.HtmlEncode(value);
}
