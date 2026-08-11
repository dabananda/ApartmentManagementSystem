using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public interface IExpenseAllocationService
{
    Task<(CommonBill? CommonBill, IReadOnlyList<ExpenseAllocation> Allocations)> GetAsync(Guid commonBillId, CancellationToken cancellationToken = default);
}
