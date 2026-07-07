namespace Fieldore.Domain.Constants;

/// <summary>
/// Canonical feature/entitlement keys. Plans grant these via <c>PlanFeature</c> rows
/// (enabled + optional numeric limit). Adding a feature = add a key here + rows in the
/// admin panel — no schema change. App, API and admin must agree on these strings.
/// </summary>
public static class FeatureKeys
{
    // Numeric-limit features (LimitValue used; null = unlimited)
    public const string JobLimit = "job_limit";                 // completed jobs per billing period
    public const string EmployeeLimit = "employee_limit";
    public const string StorageLimitMb = "storage_limit_mb";

    // Boolean capability features (IsEnabled used)
    public const string UnlimitedCustomers = "unlimited_customers";
    public const string UnlimitedQuotes = "unlimited_quotes";
    public const string UnlimitedInvoices = "unlimited_invoices";
    public const string UnlimitedVisits = "unlimited_visits";
    public const string UnlimitedScheduling = "unlimited_scheduling";
    public const string UnlimitedEmployees = "unlimited_employees";
    public const string PhotoUpload = "photo_upload";
    public const string PdfExport = "pdf_export";
    public const string Reports = "reports";
    public const string OfflineMode = "offline_mode";
    public const string GpsTracking = "gps_tracking";
    public const string TimeTracking = "time_tracking";
    public const string CustomBranding = "custom_branding";
    public const string PrioritySupport = "priority_support";
    public const string FutureAiFeatures = "future_ai_features";

    /// <summary>All known keys — used by the admin panel to render the feature matrix.</summary>
    public static readonly string[] All =
    [
        JobLimit, EmployeeLimit, StorageLimitMb,
        UnlimitedCustomers, UnlimitedQuotes, UnlimitedInvoices, UnlimitedVisits,
        UnlimitedScheduling, UnlimitedEmployees, PhotoUpload, PdfExport, Reports,
        OfflineMode, GpsTracking, TimeTracking, CustomBranding, PrioritySupport, FutureAiFeatures,
    ];
}
