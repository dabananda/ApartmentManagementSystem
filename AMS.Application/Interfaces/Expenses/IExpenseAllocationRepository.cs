using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.Expenses;

public interface IExpenseAllocationRepository
{
    Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
