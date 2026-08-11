using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public interface IExpenseAllocationService
{
    Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
