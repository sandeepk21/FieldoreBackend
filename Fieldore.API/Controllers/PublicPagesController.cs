using System.Net;
using System.Text;
using Fieldore.Application.Estimates.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

// Server-rendered, client-facing quote page (the link the provider shares).
// No JavaScript required: Accept/Reject are plain form posts.
[AllowAnonymous]
public sealed class PublicPagesController(IEstimateService estimateService) : ControllerBase
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
