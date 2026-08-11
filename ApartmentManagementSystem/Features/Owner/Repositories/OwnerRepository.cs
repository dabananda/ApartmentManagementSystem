using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Owner.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Owner.Repositories;

public sealed class OwnerRepository(ApplicationDbContext context, UserManager<ApplicationUser> users) : IOwnerRepository
{
    public async Task<OwnerDashboardData> GetDashboardDataAsync(string ownerId, DateTime monthStart, CancellationToken cancellationToken = default)
    {
        var flatsOwnedCount = await context.Flats.CountAsync(f => f.OwnerId == ownerId, cancellationToken);

        var occupiedFlatCount = await context.TenantAssignments
            .Where(a => a.EndDate == null && context.Flats.Any(f => f.Id == a.FlatId && f.OwnerId == ownerId))
            .Select(a => a.FlatId)
            .Distinct()
            .CountAsync(cancellationToken);

        var rentTotals = await context.TenantBills
            .Include(b => b.Payments)
            .Include(b => b.Flat)
            .Where(b => b.Flat!.OwnerId == ownerId)
            .Select(b => new { b.Amount, Paid = b.Payments.Sum(p => (decimal?)p.Amount) ?? 0m })
            .ToListAsync(cancellationToken);

        var rentPaidThisMonth = await context.TenantPayments
            .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)
            .Where(p => p.PaymentDate >= monthStart && p.TenantBill!.Flat!.OwnerId == ownerId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var commonTotalBilled = await context.ExpenseAllocations
            .Where(a => a.OwnerId == ownerId)
            .SumAsync(a => (decimal?)a.AmountDue, cancellationToken) ?? 0m;

        var commonTotalPaid = await context.ExpenseAllocationPayments
            .Where(p => p.OwnerId == ownerId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var tenants = await context.TenantAssignments
            .Include(a => a.TenantUser)
            .Include(a => a.Flat)
            .Where(a => a.EndDate == null && a.Flat!.OwnerId == ownerId)
            .OrderBy(a => a.Flat!.FlatNumber)
            .Select(a => new OwnerTenantRow
            {
                TenantUserId = a.TenantUserId,
                Name = a.TenantUser!.Fullname ?? a.TenantUser.UserName!,
                Email = a.TenantUser!.Email!,
                FlatNumber = a.Flat!.FlatNumber,
                From = a.StartDate
            })
            .ToListAsync(cancellationToken);

        var recentRent = await context.TenantPayments
            .Include(p => p.TenantBill)!.ThenInclude(b => b.Flat)
            .Include(p => p.TenantBill)!.ThenInclude(b => b.TenantUser)
            .Where(p => p.TenantBill!.Flat!.OwnerId == ownerId)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Take(10)
            .Select(p => new OwnerRecentRentPaymentRow
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Reference = p.Reference,
                TenantName = (p.TenantBill!.TenantUser!.Fullname ?? p.TenantBill!.TenantUser!.UserName)!,
                FlatNumber = p.TenantBill!.Flat!.FlatNumber
            })
            .ToListAsync(cancellationToken);

        var recentCommon = await context.ExpenseAllocationPayments
            .Include(p => p.ExpenseAllocation)!.ThenInclude(a => a.CommonBill)
            .Where(p => p.OwnerId == ownerId)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Take(10)
            .Select(p => new OwnerRecentCommonPaymentRow
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Reference = p.Reference,
                BillTitle = p.ExpenseAllocation!.CommonBill!.Name,
                BillDate = p.ExpenseAllocation!.CommonBill!.BillDate
            })
            .ToListAsync(cancellationToken);

        return new OwnerDashboardData(
            flatsOwnedCount,
            occupiedFlatCount,
            rentTotals.Sum(x => x.Amount),
            rentTotals.Sum(x => x.Paid),
            rentPaidThisMonth,
            commonTotalBilled,
            commonTotalPaid,
            tenants,
            recentRent,
            recentCommon);
    }

    public async Task<IReadOnlyList<OwnerOwnedFlatRow>> GetOwnedFlatsAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        return await context.Flats
            .Include(f => f.Building)
            .Where(f => f.OwnerId == ownerId)
            .Select(f => new OwnerOwnedFlatRow
            {
                FlatId = f.Id,
                FlatNumber = f.FlatNumber,
                BuildingId = f.BuildingId,
                BuildingName = f.Building!.Name,
                CurrentTenantUserId = context.TenantAssignments
                    .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                    .OrderByDescending(a => a.StartDate)
                    .Select(a => a.TenantUserId)
                    .FirstOrDefault(),
                CurrentTenantName = context.TenantAssignments
                    .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                    .OrderByDescending(a => a.StartDate)
                    .Select(a => a.TenantUser!.Fullname ?? a.TenantUser.UserName)
                    .FirstOrDefault(),
                CurrentTenantEmail = context.TenantAssignments
                    .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                    .OrderByDescending(a => a.StartDate)
                    .Select(a => a.TenantUser!.Email)
                    .FirstOrDefault(),
                TenantFrom = context.TenantAssignments
                    .Where(a => a.FlatId == f.Id && (a.EndDate == null || a.EndDate >= today))
                    .OrderByDescending(a => a.StartDate)
                    .Select(a => (DateTime?)a.StartDate)
                    .FirstOrDefault()
            })
            .OrderBy(x => x.BuildingName).ThenBy(x => x.FlatNumber)
            .ToListAsync(cancellationToken);
    }

    public async Task<OwnerBillsPage?> GetCommonBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        var q = context.ExpenseAllocations
            .Include(a => a.CommonBill)
            .Include(a => a.Payments)
            .Where(a => a.OwnerId == ownerId);

        if (restrictToBuildingId != null)
            q = q.Where(a => a.CommonBill!.BuildingId == restrictToBuildingId);

        var allocations = await q.AsNoTracking().ToListAsync(cancellationToken);
        if (allocations.Count == 0)
        {
            var owner = await users.FindByIdAsync(ownerId);
            return new OwnerBillsPage
            {
                OwnerId = ownerId,
                OwnerName = owner?.Fullname ?? owner?.UserName ?? "(owner)",
                Bills = new(),
                BuildingId = Guid.Empty,
                History = new()
            };
        }

        var buildingId = allocations.First().CommonBill!.BuildingId;
        var ownerUser = await users.FindByIdAsync(ownerId);

        var items = allocations
            .OrderByDescending(a => a.CommonBill!.BillDate)
            .Select(a => new OwnerBillItem
            {
                CommonBillId = a.CommonBillId,
                Title = a.CommonBill!.Name,
                BillDate = a.CommonBill!.BillDate,
                Allocated = a.AmountDue,
                Paid = a.Payments.Sum(p => p.Amount)
            })
            .ToList();

        var history = await context.ExpenseAllocationPayments
            .Join(context.ExpenseAllocations.Include(a => a.CommonBill),
                  p => p.ExpenseAllocationId,
                  a => a.Id,
                  (p, a) => new { p, a })
            .Where(z => z.a.OwnerId == ownerId && z.a.CommonBill!.BuildingId == buildingId)
            .OrderByDescending(z => z.p.PaymentDate).ThenByDescending(z => z.a.CommonBill!.BillDate)
            .Select(z => new OwnerPaymentRecord
            {
                PaymentId = z.p.Id,
                PaymentDate = z.p.PaymentDate,
                BillTitle = z.a.CommonBill!.Name,
                BillDate = z.a.CommonBill!.BillDate,
                Amount = z.p.Amount,
                Reference = z.p.Reference
            })
            .ToListAsync(cancellationToken);

        return new OwnerBillsPage
        {
            OwnerId = ownerId,
            OwnerName = ownerUser?.Fullname ?? ownerUser?.UserName ?? "(owner)",
            BuildingId = buildingId,
            Bills = items,
            History = history
        };
    }
}
