using ApartmentManagementSystem.Features.Expenses.Models;
using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public interface IExpensePaymentService
{
    Task<IReadOnlyList<ExpensePayment>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutstandingCommonBill>> GetOutstandingBillsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<decimal> GetRemainingAmountAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task RecordAsync(ExpensePayment payment, CancellationToken cancellationToken = default);
    Task<ExpensePayment?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
