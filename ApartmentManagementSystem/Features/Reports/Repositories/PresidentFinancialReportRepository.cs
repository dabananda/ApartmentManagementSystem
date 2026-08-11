using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Reports.Repositories;
public sealed class PresidentFinancialReportRepository(ApplicationDbContext context) : IPresidentFinancialReportRepository
{
    public async Task<IReadOnlyList<CommonBill>> GetBillsAsync(Guid buildingId, DateTime start, DateTime endExclusive, bool descending, CancellationToken cancellationToken = default)
    {
        var query = context.CommonBills.AsNoTracking().Where(bill => bill.BuildingId == buildingId && bill.BillDate >= start && bill.BillDate < endExclusive);
        return await (descending ? query.OrderByDescending(bill => bill.BillDate) : query.OrderBy(bill => bill.BillDate)).ToListAsync(cancellationToken);
    }
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetSucceededCollectionsAsync(IReadOnlyCollection<Guid> billIds, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocationPayments.AsNoTracking().Where(payment => billIds.Contains(payment.CommonBillId) && payment.Status == PaymentStatus.Succeeded).GroupBy(payment => payment.CommonBillId).Select(group => new { group.Key, Collected = group.Sum(payment => payment.Amount) }).ToDictionaryAsync(item => item.Key, item => item.Collected, cancellationToken);
    public async Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAllocationCollectionsAsync(IReadOnlyCollection<Guid> billIds, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocations.AsNoTracking().Where(allocation => billIds.Contains(allocation.CommonBillId) && allocation.IsPaid).GroupBy(allocation => allocation.CommonBillId).Select(group => new { group.Key, Collected = group.Sum(allocation => allocation.AmountDue) }).ToDictionaryAsync(item => item.Key, item => item.Collected, cancellationToken);
    public async Task<decimal> GetPaymentsTotalAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default) =>
        await context.ExpensePayments.AsNoTracking().Where(payment => payment.BuildingId == buildingId && payment.PaymentDate >= start && payment.PaymentDate < endExclusive).SumAsync(payment => (decimal?)payment.Amount, cancellationToken) ?? 0m;
}
