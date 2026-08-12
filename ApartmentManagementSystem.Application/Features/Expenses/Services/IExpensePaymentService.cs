using ApartmentManagementSystem.Application.Features.Expenses.DTOs;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Features.Expenses.Services;

public interface IExpensePaymentService
{
    Task<IReadOnlyList<ExpensePayment>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutstandingCommonBill>> GetOutstandingBillsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<decimal> GetRemainingAmountAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task RecordAsync(ExpensePayment payment, CancellationToken cancellationToken = default);
    Task<ExpensePayment?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
