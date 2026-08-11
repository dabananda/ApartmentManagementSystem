using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Features.Expenses.Repositories;

public interface IExpenseAllocationRepository
{
    Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
