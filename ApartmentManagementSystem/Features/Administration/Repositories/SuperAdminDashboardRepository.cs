using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration.Repositories;

public sealed class SuperAdminDashboardRepository(ApplicationDbContext context) : ISuperAdminDashboardRepository
{
    public async Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default) =>
        await context.Buildings.Include(building => building.Flats).Include(building => building.CommonBills).Include(building => building.ExpensePayments).ToListAsync(cancellationToken);
    public async Task<IReadOnlySet<Guid>> GetOccupiedFlatIdsAsync(CancellationToken cancellationToken = default) =>
        new HashSet<Guid>(await context.TenantAssignments.Where(assignment => assignment.EndDate == null).Select(assignment => assignment.FlatId).ToListAsync(cancellationToken));
    public async Task<(int TotalFlats, int FlatsWithOwners)> GetFlatCountsAsync(CancellationToken cancellationToken = default) =>
        (await context.Flats.CountAsync(cancellationToken), await context.Flats.CountAsync(flat => flat.OwnerId != null, cancellationToken));
    public async Task<(decimal Bills, decimal Payments, decimal Collected, decimal Allocated)> GetFinancialTotalsAsync(CancellationToken cancellationToken = default) =>
        (await context.CommonBills.SumAsync(bill => bill.TotalAmount, cancellationToken), await context.ExpensePayments.SumAsync(payment => payment.Amount, cancellationToken), await context.ExpenseAllocationPayments.SumAsync(payment => payment.Amount, cancellationToken), await context.ExpenseAllocations.SumAsync(allocation => allocation.AmountDue, cancellationToken));
    public async Task<IReadOnlyList<CommonBill>> GetRecentBillsAsync(CancellationToken cancellationToken = default) =>
        await context.CommonBills.Include(bill => bill.Building).OrderByDescending(bill => bill.BillDate).Take(5).ToListAsync(cancellationToken);
    public async Task<IReadOnlyList<ExpensePayment>> GetRecentPaymentsAsync(CancellationToken cancellationToken = default) =>
        await context.ExpensePayments.Include(payment => payment.Building).Include(payment => payment.CommonBill).OrderByDescending(payment => payment.PaymentDate).Take(5).ToListAsync(cancellationToken);
}
