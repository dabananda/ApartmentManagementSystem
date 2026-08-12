using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Expenses.Services;

public interface IExpenseAllocationService
{
    Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
