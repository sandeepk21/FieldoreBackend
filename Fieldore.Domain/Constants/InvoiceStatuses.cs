namespace Fieldore.Domain.Constants;

public static class InvoiceStatuses
{
    public const string Draft = "draft";
    public const string Sent = "sent";
    public const string Viewed = "viewed";
    public const string PartiallyPaid = "partially_paid";
    public const string Paid = "paid";
    public const string Overdue = "overdue";
    public const string Void = "void";

    // Legacy value kept for backward compatibility with existing rows; no longer produced.
    public const string Unpaid = "unpaid";
}
