using ApartmentManagementSystem.Features.Expenses.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public sealed class ExpenseAllocationService(IExpenseAllocationRepository allocations) : IExpenseAllocationService
{
    public async Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default)
    {
        var bill = await allocations.GetCommonBillAsync(commonBillId, cancellationToken);
        if (bill is null) return (null, Array.Empty<ExpenseAllocation>());
        return (bill, await allocations.GetAllocationsAsync(commonBillId, cancellationToken));
    }
}
