using AMS.Application.Interfaces.Administration;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Administration;

public sealed class SuperAdminDashboardRepository(ApplicationDbContext context) : ISuperAdminDashboardRepository
{
    public async Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default) =>
        await context.Buildings.AsNoTracking().Include(building => building.Flats).Include(building => building.CommonBills).Include(building => building.ExpensePayments).AsSplitQuery().ToListAsync(cancellationToken);
    public async Task<IReadOnlySet<Guid>> GetOccupiedFlatIdsAsync(CancellationToken cancellationToken = default) =>
        new HashSet<Guid>(await context.TenantAssignments.AsNoTracking().Where(assignment => assignment.EndDate == null).Select(assignment => assignment.FlatId).ToListAsync(cancellationToken));
    public async Task<(int TotalFlats, int FlatsWithOwners)> GetFlatCountsAsync(CancellationToken cancellationToken = default) =>
        (await context.Flats.CountAsync(cancellationToken), await context.Flats.CountAsync(flat => flat.OwnerId != null, cancellationToken));
    public async Task<(decimal Bills, decimal Payments, decimal Collected, decimal Allocated)> GetFinancialTotalsAsync(CancellationToken cancellationToken = default) =>
        (await context.CommonBills.SumAsync(bill => bill.TotalAmount, cancellationToken), await context.ExpensePayments.SumAsync(payment => payment.Amount, cancellationToken), await context.ExpenseAllocationPayments.SumAsync(payment => payment.Amount, cancellationToken), await context.ExpenseAllocations.SumAsync(allocation => allocation.AmountDue, cancellationToken));
    public async Task<IReadOnlyList<CommonBill>> GetRecentBillsAsync(CancellationToken cancellationToken = default) =>
        await context.CommonBills.AsNoTracking().Include(bill => bill.Building).OrderByDescending(bill => bill.BillDate).Take(5).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ExpensePayment>> GetRecentPaymentsAsync(CancellationToken cancellationToken = default) =>
        await context.ExpensePayments.AsNoTracking().Include(payment => payment.Building).Include(payment => payment.CommonBill).OrderByDescending(payment => payment.PaymentDate).Take(5).ToListAsync(cancellationToken);
}
