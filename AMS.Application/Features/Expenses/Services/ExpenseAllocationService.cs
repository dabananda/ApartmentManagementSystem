using AMS.Application.Interfaces.Expenses;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Expenses.Services;

public sealed class ExpenseAllocationService(IExpenseAllocationRepository allocations) : IExpenseAllocationService
{
    public async Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default)
    {
        var bill = await allocations.GetCommonBillAsync(commonBillId, cancellationToken);
        if (bill is null) return (null, Array.Empty<ExpenseAllocation>());
        return (bill, await allocations.GetAllocationsAsync(commonBillId, cancellationToken));
    }
}
