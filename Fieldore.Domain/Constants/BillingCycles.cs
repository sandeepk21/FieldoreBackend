namespace Fieldore.Domain.Constants;

public static class BillingCycles
{
    public const string Monthly = "monthly";
    public const string HalfYearly = "half_yearly";

    // Reserved for future use — schema already supports these without redesign.
    public const string Quarterly = "quarterly";
    public const string Yearly = "yearly";

    /// <summary>Cycles offered at launch.</summary>
    public static readonly string[] Active = [Monthly, HalfYearly];

    /// <summary>Number of months a cycle spans (used for period/renewal math).</summary>
    public static int MonthsFor(string cycle) => cycle switch
    {
        Monthly => 1,
        Quarterly => 3,
        HalfYearly => 6,
        Yearly => 12,
        _ => 1,
    };
}
