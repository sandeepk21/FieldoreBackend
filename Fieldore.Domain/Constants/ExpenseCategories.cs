namespace Fieldore.Domain.Constants;

public static class ExpenseCategories
{
    public const string Fuel = "fuel";
    public const string Materials = "materials";
    public const string Labor = "labor";
    public const string Equipment = "equipment";
    public const string Subcontractor = "subcontractor";
    public const string Other = "other";

    public static readonly string[] All =
    [
        Fuel, Materials, Labor, Equipment, Subcontractor, Other
    ];
}
