using AMS.Application.Interfaces.Expenses;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Application.Features.Expenses.DTOs;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Expenses;

public sealed class ExpensePaymentRepository(ApplicationDbContext context) : IExpensePaymentRepository
{
    public async Task<IReadOnlyList<ExpensePayment>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.ExpensePayments.Include(payment => payment.CommonBill).Where(payment => payment.BuildingId == buildingId)
            .OrderByDescending(payment => payment.PaymentDate).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OutstandingCommonBill>> GetOutstandingBillsAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var paidAmounts = await context.ExpensePayments.Where(payment => payment.BuildingId == buildingId)
            .GroupBy(payment => payment.CommonBillId).Select(group => new { group.Key, PaidAmount = group.Sum(payment => payment.Amount) }).ToListAsync(cancellationToken);
        var bills = await context.CommonBills.Where(bill => bill.BuildingId == buildingId).ToListAsync(cancellationToken);
        return bills.Select(bill => new OutstandingCommonBill(bill.Id, bill.Name, bill.TotalAmount - (paidAmounts.FirstOrDefault(payment => payment.Key == bill.Id)?.PaidAmount ?? 0)))
            .Where(bill => bill.Outstanding > 0).ToList();
    }

    public Task<decimal> GetPaidAmountAsync(Guid commonBillId, CancellationToken cancellationToken = default) =>
        context.ExpensePayments.Where(payment => payment.CommonBillId == commonBillId).SumAsync(payment => payment.Amount, cancellationToken);

    public Task<CommonBill?> GetBillAsync(Guid commonBillId, CancellationToken cancellationToken = default) => context.CommonBills.FindAsync([commonBillId], cancellationToken).AsTask();

    public async Task AddAsync(ExpensePayment payment, CancellationToken cancellationToken = default)
    {
        context.Add(payment);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task<ExpensePayment?> GetAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.ExpensePayments.Include(payment => payment.CommonBill).FirstOrDefaultAsync(payment => payment.Id == id, cancellationToken);
}
