using ApartmentManagementSystem.Application.Features.Expenses.DTOs;
using ApartmentManagementSystem.Application.Interfaces.Expenses;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Features.Expenses.Services;

public sealed class ExpensePaymentService(IExpensePaymentRepository payments) : IExpensePaymentService
{
    public Task<IReadOnlyList<ExpensePayment>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) => payments.GetByBuildingAsync(buildingId, cancellationToken);
    public Task<IReadOnlyList<OutstandingCommonBill>> GetOutstandingBillsAsync(Guid buildingId, CancellationToken cancellationToken = default) => payments.GetOutstandingBillsAsync(buildingId, cancellationToken);
    public Task<ExpensePayment?> GetAsync(Guid id, CancellationToken cancellationToken = default) => payments.GetAsync(id, cancellationToken);
    public Task RecordAsync(ExpensePayment payment, CancellationToken cancellationToken = default) => payments.AddAsync(payment, cancellationToken);

    public async Task<decimal> GetRemainingAmountAsync(Guid commonBillId, CancellationToken cancellationToken = default)
    {
        var paidSoFar = await payments.GetPaidAmountAsync(commonBillId, cancellationToken);
        var bill = await payments.GetBillAsync(commonBillId, cancellationToken);
        return bill!.TotalAmount - paidSoFar;
    }
}
