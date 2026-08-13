using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.TenantPortal;

public sealed class TenantPortalRepository(ApplicationDbContext context, UserManager<ApplicationUser> users) : ITenantPortalRepository
{
    public async Task<(TenantAssignment? assignment, ApplicationUser? me)> GetActiveAssignmentAsync(string tenantUserId, CancellationToken cancellationToken = default)
    {
        var me = await users.FindByIdAsync(tenantUserId);
        var today = DateTime.Today;
        var assignment = await context.TenantAssignments
            .AsNoTracking()
            .Include(a => a.Flat).ThenInclude(f => f!.Building)
            .Where(a => a.TenantUserId == tenantUserId && (a.EndDate == null || a.EndDate >= today))
            .OrderByDescending(a => a.StartDate)
            .FirstOrDefaultAsync(cancellationToken);

        return (assignment, me);
    }

    public async Task<TenantDashboardVM?> GetDashboardDataAsync(string tenantUserId, CancellationToken cancellationToken = default)
    {
        var (assignment, me) = await GetActiveAssignmentAsync(tenantUserId, cancellationToken);
        if (assignment?.Flat == null || me == null) return null;

        var today = DateTime.Today;
        var buildingId = assignment.Flat.BuildingId;

        var bills = await context.TenantBills
            .AsNoTracking()
            .Include(b => b.Payments)
            .Where(b => b.TenantUserId == tenantUserId)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

        var total = bills.Sum(b => b.Amount);
        var paid = bills.Sum(b => b.Payments.Sum(p => p.Amount));

        var monthStart = new DateTime(today.Year, today.Month, 1);
        var paidThisMonth = await context.TenantPayments
            .Where(p => p.PaymentDate >= monthStart && p.TenantBill!.TenantUserId == tenantUserId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var recentBills = bills.Take(6).Select(b => new TenantBillRow
        {
            BillId = b.Id,
            Title = b.Title,
            BillDate = b.BillDate,
            Amount = b.Amount,
            Paid = b.Payments.Sum(p => p.Amount)
        }).ToList();

        var recentPayments = await context.TenantPayments
            .AsNoTracking()
            .Include(p => p.TenantBill)
            .Where(p => p.TenantBill!.TenantUserId == tenantUserId)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Take(6)
            .Select(p => new TenantPaymentRow
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Reference = p.Reference,
                BillTitle = p.TenantBill!.Title,
                BillDate = p.TenantBill!.BillDate
            })
            .ToListAsync(cancellationToken);

        var notices = await context.Announcements
            .AsNoTracking()
            .Where(a => a.BuildingId == buildingId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(6)
            .ToListAsync(cancellationToken);

        return new TenantDashboardVM
        {
            TenantName = me.Fullname ?? me.UserName ?? "Me",
            BuildingName = assignment.Flat.Building!.Name,
            FlatNumber = assignment.Flat.FlatNumber,

            TotalBilled = total,
            TotalPaid = paid,
            PaidThisMonth = paidThisMonth,

            RecentBills = recentBills,
            RecentPayments = recentPayments,
            RecentNotices = notices
        };
    }

    public async Task<IEnumerable<TenantBillRow>> GetBillsAsync(string tenantUserId, CancellationToken cancellationToken = default) => await context.TenantBills
            .AsNoTracking()
            .Include(b => b.Payments)
            .Include(b => b.Flat).ThenInclude(f => f!.Building)
            .Where(b => b.TenantUserId == tenantUserId)
            .OrderByDescending(b => b.BillDate)
            .Select(b => new TenantBillRow
            {
                BillId = b.Id,
                Title = b.Title,
                BillDate = b.BillDate,
                Amount = b.Amount,
                Paid = b.Payments.Sum(p => p.Amount),
                BuildingName = b.Flat!.Building!.Name,
                FlatNumber = b.Flat!.FlatNumber
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<TenantPaymentRow>> GetPaymentsAsync(string tenantUserId, CancellationToken cancellationToken = default) => await context.TenantPayments
            .AsNoTracking()
            .Include(p => p.TenantBill).ThenInclude(b => b!.Flat).ThenInclude(f => f!.Building)
            .Where(p => p.TenantBill!.TenantUserId == tenantUserId)
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Select(p => new TenantPaymentRow
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                Amount = p.Amount,
                Reference = p.Reference,
                BillTitle = p.TenantBill!.Title,
                BillDate = p.TenantBill!.BillDate,
                BuildingName = p.TenantBill!.Flat!.Building!.Name,
                FlatNumber = p.TenantBill!.Flat!.FlatNumber
            })
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Announcement>> GetNoticesAsync(Guid? buildingId, CancellationToken cancellationToken = default) => await context.Announcements
            .AsNoTracking()
            .Where(a => !buildingId.HasValue || a.BuildingId == buildingId.Value)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<MaintenanceTicket>> GetTicketsAsync(Guid buildingId, Guid flatId, string tenantUserId, CancellationToken cancellationToken = default) => await context.MaintenanceTickets
            .AsNoTracking()
            .Where(t => t.BuildingId == buildingId && (t.CreatedByUserId == tenantUserId || (t.CreatedByUserId == null && t.FlatId == flatId)))
            .OrderBy(t => t.Status).ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);

    public async Task CreateTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default)
    {
        context.MaintenanceTickets.Add(ticket);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IEnumerable<EntryLog>> GetVisitorsAsync(Guid buildingId, Guid flatId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) {
        var q = context.EntryLogs.AsNoTracking()
            .Where(el => el.FlatId == flatId && el.BuildingId == buildingId);

        if (from.HasValue) q = q.Where(el => el.EntryTime >= from.Value);
        if (to.HasValue) q = q.Where(el => el.EntryTime <= to.Value);

        return await q.OrderByDescending(el => el.EntryTime).ToListAsync(cancellationToken);
    }
}


