using Fieldore.Application.Auth.Contracts;

namespace Fieldore.Application.Expenses.Contracts;

public interface IExpenseService
{
    Task<ApiResponse<List<ExpenseResponse>>> GetAllAsync(Guid userId, GetExpensesRequest request, CancellationToken ct = default);
    Task<ApiResponse<ExpenseSummaryResponse>> GetSummaryAsync(Guid userId, CancellationToken ct = default);
    Task<ApiResponse<ExpenseResponse>> CreateAsync(Guid userId, CreateExpenseRequest request, CancellationToken ct = default);
    Task<ApiResponse<ExpenseResponse>> UpdateAsync(Guid userId, Guid expenseId, UpdateExpenseRequest request, CancellationToken ct = default);
    Task<ApiResponse<bool>> DeleteAsync(Guid userId, Guid expenseId, CancellationToken ct = default);
}
