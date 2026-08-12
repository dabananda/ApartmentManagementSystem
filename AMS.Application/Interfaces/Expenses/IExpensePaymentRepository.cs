using AMS.Application.Features.Expenses.DTOs;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Interfaces.Expenses;

public interface IExpensePaymentRepository
{
    Task<IReadOnlyList<ExpensePayment>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OutstandingCommonBill>> GetOutstandingBillsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<decimal> GetPaidAmountAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task<CommonBill?> GetBillAsync(Guid commonBillId, CancellationToken cancellationToken = default);
    Task AddAsync(ExpensePayment payment, CancellationToken cancellationToken = default);
    Task<ExpensePayment?> GetAsync(Guid id, CancellationToken cancellationToken = default);
}
