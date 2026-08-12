using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Expenses;

public interface IExpenseAllocationRepository
{
    Task<CommonBill?> GetCommonBillAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpenseAllocation>> GetAllocationsAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
