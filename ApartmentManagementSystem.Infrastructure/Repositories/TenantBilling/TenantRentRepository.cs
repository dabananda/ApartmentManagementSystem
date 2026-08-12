using ApartmentManagementSystem.Application.Interfaces.TenantBilling;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Tenancy.DTOs;
using ApartmentManagementSystem.Application.Features.TenantPortal.DTOs;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApartmentManagementSystem.Infrastructure.Repositories.TenantBilling;

public sealed class TenantRentRepository(ApplicationDbContext context, UserManager<ApplicationUser> users) : ITenantRentRepository
{
    public async Task<List<TenantRentListRow>> GetTenantRentListAsync(string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        var q = context.TenantAssignments.AsNoTracking()
            .Include(a => a.Flat)
            .Include(a => a.TenantUser)
            .Where(a => a.EndDate == null);

        if (!string.IsNullOrWhiteSpace(restrictToOwnerId))
            q = q.Where(a => a.Flat!.OwnerId == restrictToOwnerId);

        return await q
            .GroupBy(a => new
            {
                a.TenantUserId,
                Fullname = a.TenantUser!.Fullname,
                UserName = a.TenantUser!.UserName,
                Email = a.TenantUser!.Email
            })
            .Select(g => new TenantRentListRow
            {
                TenantUserId = g.Key.TenantUserId!,
                Name = (g.Key.Fullname ?? g.Key.UserName)!,
                Email = g.Key.Email!,
                BuildingId = g.Max(x => x.Flat!.BuildingId)
            })
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> IsTenantVisibleToOwnerAsync(string tenantUserId, string ownerId, CancellationToken cancellationToken = default) =>
        context.TenantAssignments.Include(a => a.Flat)
            .AnyAsync(a => a.TenantUserId == tenantUserId && a.Flat!.OwnerId == ownerId, cancellationToken);

    public async Task<int> EnsureCurrentMonthBillsForTenantAsync(string tenantUserId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var firstOfMonth = new DateTime(today.Year, today.Month, 1);

        var activeAssignments = await context.TenantAssignments
            .Include(a => a.Flat)
            .Where(a => a.TenantUserId == tenantUserId && (a.EndDate == null || a.EndDate >= today))
            .ToListAsync(cancellationToken);

        if (activeAssignments.Count == 0) return 0;

        var flatIds = activeAssignments.Select(a => a.FlatId).Distinct().ToList();

        var profiles = await context.FlatBillingProfiles
            .Where(p => flatIds.Contains(p.FlatId) && p.IsActive)
            .ToListAsync(cancellationToken);

        if (profiles.Count == 0) return 0;

        var existingBills = await context.TenantBills
            .Where(b => b.TenantUserId == tenantUserId && b.BillDate == firstOfMonth)
            .Select(b => new { b.FlatId })
            .ToListAsync(cancellationToken);

        var existingFlatIds = existingBills.Select(b => b.FlatId).ToHashSet();

        int created = 0;
        foreach (var prof in profiles)
        {
            var assignment = activeAssignments
                .Where(a => a.FlatId == prof.FlatId)
                .OrderByDescending(a => a.StartDate)
                .FirstOrDefault();

            if (assignment == null) continue;

            var startMonth = new DateTime(assignment.StartDate.Year, assignment.StartDate.Month, 1);
            if (startMonth > firstOfMonth) continue;

            if (existingFlatIds.Contains(prof.FlatId)) continue;

            await context.TenantBills.AddAsync(new TenantBill
            {
                FlatId = prof.FlatId,
                TenantUserId = tenantUserId,
                Title = string.IsNullOrWhiteSpace(prof.Title) ? "Monthly Rent" : prof.Title,
                BillDate = firstOfMonth,
                Amount = prof.MonthlyAmount
            }, cancellationToken);
            created++;
        }

        if (created > 0) await context.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<TenantBillsPage?> GetTenantBillsPageAsync(string tenantUserId, CancellationToken cancellationToken = default)
    {
        var bills = await context.TenantBills
            .Include(b => b.Payments)
            .Include(b => b.Flat)
            .Where(b => b.TenantUserId == tenantUserId)
            .OrderByDescending(b => b.BillDate)
            .ToListAsync(cancellationToken);

        if (bills.Count == 0) return null;

        var tenant = await users.FindByIdAsync(tenantUserId);

        return new TenantBillsPage
        {
            TenantUserId = tenantUserId,
            TenantName = tenant?.Fullname ?? tenant?.UserName ?? "(no name)",
            Email = tenant?.Email ?? "",
            BuildingId = bills.First().Flat!.BuildingId,
            Bills = bills.Select(b => new TenantBillRow
            {
                BillId = b.Id,
                Title = b.Title,
                BillDate = b.BillDate,
                Amount = b.Amount,
                Paid = b.Payments.Sum(p => p.Amount)
            }).ToList()
        };
    }

    public Task<List<TenantPaymentRecord>> GetTenantPaymentHistoryAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        context.TenantPayments
            .Include(p => p.TenantBill)
            .Where(p => p.TenantBill!.TenantUserId == tenantUserId)
            .OrderByDescending(p => p.PaymentDate)
            .Select(p => new TenantPaymentRecord
            {
                PaymentId = p.Id,
                PaymentDate = p.PaymentDate,
                BillTitle = p.TenantBill!.Title,
                BillDate = p.TenantBill.BillDate,
                Amount = p.Amount,
                Reference = p.Reference
            }).ToListAsync(cancellationToken);

    public async Task<(TenantPayment? payment, string ownerId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var p = await context.TenantPayments
            .Include(x => x.TenantBill)!.ThenInclude(b => b.Flat)!.ThenInclude(f => f.Building)
            .Include(x => x.TenantBill)!.ThenInclude(b => b.TenantUser)
            .FirstOrDefaultAsync(x => x.Id == paymentId, cancellationToken);

        if (p == null) return (null, string.Empty);
        return (p, p.TenantBill!.Flat!.OwnerId ?? string.Empty);
    }

    public Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default) =>
        context.TenantPayments.AsNoTracking().AnyAsync(p => p.IdempotencyKey == key, cancellationToken);

    public async Task<(List<TenantPayment> payments, string? tenantUserId)> RecordPayAsync(RecordTenantPaymentVM vm, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var bill = await context.TenantBills.Include(b => b.Flat).FirstOrDefaultAsync(b => b.Id == vm.TenantBillId, cancellationToken);
        if (bill == null) { await tx.RollbackAsync(cancellationToken); return ([], null); }
        if (!string.IsNullOrWhiteSpace(restrictToOwnerId) && bill.Flat!.OwnerId != restrictToOwnerId) { await tx.RollbackAsync(cancellationToken); return ([], bill.TenantUserId); }

        var paidNow = await context.TenantPayments.Where(p => p.TenantBillId == bill.Id).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        var dueNow = bill.Amount - paidNow;
        if (dueNow <= 0) { await tx.RollbackAsync(cancellationToken); return ([], bill.TenantUserId); }

        var take = Math.Min(vm.Amount, dueNow);
        if (take <= 0) { await tx.RollbackAsync(cancellationToken); return ([], bill.TenantUserId); }

        var entity = new TenantPayment
        {
            TenantBillId = bill.Id,
            Amount = take,
            PaymentDate = vm.PaymentDate,
            Reference = vm.Reference,
            IdempotencyKey = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? null : vm.IdempotencyKey,
            Gateway = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? PaymentGateway.None : PaymentGateway.Stripe,
            Status = PaymentStatus.Succeeded
        };

        await context.TenantPayments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ([entity], bill.TenantUserId);
    }

    public async Task<(List<TenantPayment> payments, string? tenantUserId)> RecordFullPayAsync(Guid billId, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var bill = await context.TenantBills.Include(b => b.Flat).FirstOrDefaultAsync(b => b.Id == billId, cancellationToken);
        if (bill == null) { await tx.RollbackAsync(cancellationToken); return ([], null); }
        if (!string.IsNullOrWhiteSpace(restrictToOwnerId) && bill.Flat!.OwnerId != restrictToOwnerId) { await tx.RollbackAsync(cancellationToken); return ([], bill.TenantUserId); }

        var paidNow = await context.TenantPayments.Where(p => p.TenantBillId == bill.Id).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
        var dueNow = bill.Amount - paidNow;
        if (dueNow <= 0) { await tx.RollbackAsync(cancellationToken); return ([], bill.TenantUserId); }

        var entity = new TenantPayment
        {
            TenantBillId = bill.Id,
            Amount = dueNow,
            PaymentDate = DateTime.Today,
            Reference = "FullPay",
            Status = PaymentStatus.Succeeded
        };

        await context.TenantPayments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return ([entity], bill.TenantUserId);
    }

    public async Task<(List<TenantPayment> payments, string? tenantUserId)> RecordPayAllAsync(string tenantUserId, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var q = context.TenantBills.Include(b => b.Flat).Where(b => b.TenantUserId == tenantUserId);
        if (!string.IsNullOrWhiteSpace(restrictToOwnerId))
            q = q.Where(b => b.Flat!.OwnerId == restrictToOwnerId);

        var bills = await q.OrderBy(b => b.BillDate).ToListAsync(cancellationToken);
        if (bills.Count == 0) { await tx.RollbackAsync(cancellationToken); return ([], tenantUserId); }

        var created = new List<TenantPayment>();
        foreach (var b in bills)
        {
            var paidNow = await context.TenantPayments.Where(p => p.TenantBillId == b.Id).SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;
            var due = b.Amount - paidNow;
            if (due <= 0) continue;

            var e = new TenantPayment
            {
                TenantBillId = b.Id,
                Amount = due,
                PaymentDate = DateTime.Today,
                Reference = "PayAll",
                Status = PaymentStatus.Succeeded
            };
            await context.TenantPayments.AddAsync(e, cancellationToken);
            created.Add(e);
        }

        if (created.Count == 0) { await tx.RollbackAsync(cancellationToken); return ([], tenantUserId); }

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (created, tenantUserId);
    }
}
