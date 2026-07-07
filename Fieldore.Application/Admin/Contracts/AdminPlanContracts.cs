using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Admin.Contracts;

public sealed record AdminPlanPriceDto(Guid? Id, string BillingCycle, decimal Amount, string Currency, bool IsActive, string? StripePriceId);

public sealed record AdminPlanFeatureDto(
    Guid? Id, string FeatureKey, bool IsEnabled, int? LimitValue, string? DisplayLabel, int DisplayOrder, bool ShowOnPricing);

public sealed record AdminPlanResponse(
    Guid Id, string Name, string Slug, string? Description, string Currency,
    bool IsActive, bool IsArchived, bool IsRecommended, string Visibility, int DisplayOrder,
    string? Badge, string ButtonText, string? Color, int TrialDays,
    int ActiveSubscriberCount,
    List<AdminPlanPriceDto> Prices, List<AdminPlanFeatureDto> Features);

public sealed record CreatePlanRequest(
    string Name, string? Slug, string? Description, string Currency,
    string? Badge, string? ButtonText, string? Color, int TrialDays,
    string Visibility, bool IsRecommended,
    List<AdminPlanPriceDto> Prices, List<AdminPlanFeatureDto> Features);

public sealed record UpdatePlanRequest(
    string Name, string? Description, string Currency,
    string? Badge, string? ButtonText, string? Color, int TrialDays,
    string Visibility, bool IsRecommended);

public sealed record ReplacePricesRequest(List<AdminPlanPriceDto> Prices);
public sealed record ReplaceFeaturesRequest(List<AdminPlanFeatureDto> Features);
public sealed record PlanOrderDto(Guid Id, int DisplayOrder);
public sealed record ReorderPlansRequest(List<PlanOrderDto> Items);

public interface IAdminPlanService
{
    Task<ApiResponse<List<AdminPlanResponse>>> ListAsync(CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> GetAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> CreateAsync(CreatePlanRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> UpdateAsync(Guid id, UpdatePlanRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> ReplacePricesAsync(Guid id, ReplacePricesRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> ReplaceFeaturesAsync(Guid id, ReplaceFeaturesRequest request, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> SetStateAsync(Guid id, string action, CancellationToken ct = default); // enable|disable|archive|unarchive|recommend
    Task<ApiResponse<AdminPlanResponse>> DuplicateAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<bool>> ReorderAsync(ReorderPlansRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<ApiResponse<AdminPlanResponse>> SyncToStripeAsync(Guid id, CancellationToken ct = default);
}
