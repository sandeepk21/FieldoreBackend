using System.Security.Claims;
using Fieldore.Application.Expenses.Contracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Fieldore.API.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public sealed class ExpensesController(IExpenseService expenseService) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
    {
        var raw = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(raw, out userId);
    }

    [HttpGet("getAll")]
    public async Task<IActionResult> GetAll(
        [FromQuery] Guid? jobId,
        [FromQuery] string? category,
        [FromQuery] DateOnly? dateFrom,
        [FromQuery] DateOnly? dateTo,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        var request = new GetExpensesRequest
        {
            JobId = jobId,
            Category = category,
            DateFrom = dateFrom,
            DateTo = dateTo,
        };
        return Ok(await expenseService.GetAllAsync(userId, request, cancellationToken));
    }

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await expenseService.GetSummaryAsync(userId, cancellationToken));
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create(
        [FromBody] CreateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await expenseService.CreateAsync(userId, request, cancellationToken));
    }

    [HttpPut("update/{expenseId:guid}")]
    public async Task<IActionResult> Update(
        Guid expenseId,
        [FromBody] UpdateExpenseRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await expenseService.UpdateAsync(userId, expenseId, request, cancellationToken));
    }

    [HttpDelete("{expenseId:guid}")]
    public async Task<IActionResult> Delete(
        Guid expenseId,
        CancellationToken cancellationToken)
    {
        if (!TryGetUserId(out var userId)) return Unauthorized();
        return Ok(await expenseService.DeleteAsync(userId, expenseId, cancellationToken));
    }
}
