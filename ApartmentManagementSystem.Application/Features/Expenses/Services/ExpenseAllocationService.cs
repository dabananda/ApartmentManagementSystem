using ApartmentManagementSystem.Application.Interfaces.Expenses;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Features.Expenses.Services;

public sealed class ExpenseAllocationService(IExpenseAllocationRepository allocations) : IExpenseAllocationService
{
    public async Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default)
    {
        var bill = await allocations.GetCommonBillAsync(commonBillId, cancellationToken);
        if (bill is null) return (null, Array.Empty<ExpenseAllocation>());
        return (bill, await allocations.GetAllocationsAsync(commonBillId, cancellationToken));
    }
}
