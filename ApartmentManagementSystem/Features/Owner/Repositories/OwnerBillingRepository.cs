using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Owner.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data;

namespace ApartmentManagementSystem.Features.Owner.Repositories;

public sealed class OwnerBillingRepository(ApplicationDbContext context, UserManager<ApplicationUser> users) : IOwnerBillingRepository
{
    public async Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var owners = await context.Flats.AsNoTracking()
            .Include(f => f.Owner)
            .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
            .Select(f => new { f.OwnerId, Name = f.Owner!.Fullname })
            .Distinct()
            .ToListAsync(cancellationToken);

        var flatsCsv = await context.Flats.AsNoTracking()
            .Where(f => f.BuildingId == buildingId && f.OwnerId != null)
            .GroupBy(f => f.OwnerId!)
            .Select(g => new { OwnerId = g.Key, Csv = string.Join(", ", g.OrderBy(x => x.FlatNumber).Select(x => x.FlatNumber)) })
            .ToDictionaryAsync(x => x.OwnerId, x => x.Csv, cancellationToken);

        var allocAggList = await context.ExpenseAllocations.AsNoTracking()
            .Include(a => a.CommonBill)
            .Where(a => a.CommonBill!.BuildingId == buildingId)
            .GroupBy(a => a.OwnerId)
            .Select(g => new { OwnerId = g.Key, Alloc = g.Sum(x => x.AmountDue) })
            .ToListAsync(cancellationToken);
        var allocAgg = allocAggList.ToDictionary(x => x.OwnerId, x => x.Alloc);

        var paidAggList = await context.ExpenseAllocationPayments.AsNoTracking()
            .Join(context.ExpenseAllocations.Include(a => a.CommonBill).Where(a => a.CommonBill!.BuildingId == buildingId),
                p => p.ExpenseAllocationId, a => a.Id, (p, a) => new { a.OwnerId, p.Amount })
            .GroupBy(x => x.OwnerId)
            .Select(g => new { OwnerId = g.Key!, Paid = g.Sum(x => x.Amount) })
            .ToListAsync(cancellationToken);
        var paidAgg = paidAggList.ToDictionary(x => x.OwnerId, x => x.Paid);

        return owners.Select(o => new OwnerBillingRow
        {
            OwnerId = o.OwnerId!,
            OwnerName = string.IsNullOrWhiteSpace(o.Name) ? "(no name)" : o.Name!,
            FlatsCsv = flatsCsv.TryGetValue(o.OwnerId!, out var csv) ? csv : "",
            TotalAllocated = allocAgg.TryGetValue(o.OwnerId!, out var alloc) ? alloc : 0m,
            TotalPaid = paidAgg.TryGetValue(o.OwnerId!, out var paid) ? paid : 0m
        }).OrderBy(r => r.OwnerName).ToList();
    }

    public async Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        var q = context.ExpenseAllocations
            .Include(a => a.CommonBill)
            .Include(a => a.Payments)
            .Where(a => a.OwnerId == ownerId);

        if (restrictToBuildingId != null)
            q = q.Where(a => a.CommonBill!.BuildingId == restrictToBuildingId);

        var allocations = await q.AsNoTracking().ToListAsync(cancellationToken);
        if (allocations.Count == 0) return null;

        var buildingId = allocations.First().CommonBill!.BuildingId;
        var owner = await users.FindByIdAsync(ownerId);

        var items = allocations
            .OrderByDescending(a => a.CommonBill!.BillDate)
            .Select(a => new OwnerBillItem
            {
                CommonBillId = a.CommonBillId,
                Title = a.CommonBill!.Name,
                BillDate = a.CommonBill!.BillDate,
                Allocated = a.AmountDue,
                Paid = a.Payments.Sum(p => p.Amount)
            }).ToList();

        var paymentsQ = context.ExpenseAllocationPayments
            .Join(context.ExpenseAllocations.Include(a => a.CommonBill),
                p => p.ExpenseAllocationId, a => a.Id, (p, a) => new { p, a })
            .Where(z => z.a.OwnerId == ownerId && z.a.CommonBill!.BuildingId == buildingId);

        var history = await paymentsQ
            .OrderByDescending(z => z.p.PaymentDate).ThenByDescending(z => z.a.CommonBill!.BillDate)
            .Select(z => new OwnerPaymentRecord
            {
                PaymentId = z.p.Id,
                PaymentDate = z.p.PaymentDate,
                BillTitle = z.a.CommonBill!.Name,
                BillDate = z.a.CommonBill!.BillDate,
                Amount = z.p.Amount,
                Reference = z.p.Reference
            }).ToListAsync(cancellationToken);

        return new OwnerBillsPage
        {
            OwnerId = ownerId,
            OwnerName = owner?.Fullname ?? "(no name)",
            BuildingId = buildingId,
            Bills = items,
            History = history
        };
    }

    public async Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var result = await context.ExpenseAllocationPayments
            .Where(x => x.Id == paymentId)
            .Join(context.ExpenseAllocations.Include(a => a.CommonBill).Include(a => a.Owner),
                p => p.ExpenseAllocationId, a => a.Id,
                (p, a) => new { Payment = p, BuildingId = (Guid?)a.CommonBill!.BuildingId })
            .FirstOrDefaultAsync(cancellationToken);

        if (result == null) return (null, null);

        var payment = await context.ExpenseAllocationPayments
            .Include(p => p.ExpenseAllocation)!.ThenInclude(a => a!.CommonBill)!.ThenInclude(b => b!.Building)
            .Include(p => p.ExpenseAllocation)!.ThenInclude(a => a!.Owner)
            .AsSplitQuery()
            .FirstOrDefaultAsync(p => p.Id == paymentId, cancellationToken);

        return (payment, result.BuildingId);
    }

    public Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default) =>
        context.ExpenseAllocationPayments.AsNoTracking().AnyAsync(p => p.IdempotencyKey == key, cancellationToken);

    public async Task<List<ExpenseAllocation>> GetAllocationsForPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        var q = context.ExpenseAllocations.Include(a => a.CommonBill)
            .Where(a => a.CommonBillId == commonBillId && a.OwnerId == ownerId);
        if (restrictToBuildingId != null)
            q = q.Where(a => a.CommonBill!.BuildingId == restrictToBuildingId);
        return await q.OrderBy(a => a.Id).ToListAsync(cancellationToken);
    }

    public Task<decimal> GetPaidForAllocationAsync(Guid allocationId, CancellationToken cancellationToken = default) =>
        context.ExpenseAllocationPayments
            .Where(p => p.ExpenseAllocationId == allocationId)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken)
            .ContinueWith(t => t.Result ?? 0m);

    public async Task<List<ExpenseAllocationPayment>> RecordPayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var allocs = await GetAllocationsForPayAsync(ownerId, commonBillId, restrictToBuildingId, cancellationToken);
        if (allocs.Count == 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return [];
        }

        var totalDueNow = 0m;
        foreach (var a in allocs)
        {
            var paid = await GetPaidForAllocationAsync(a.Id, cancellationToken);
            totalDueNow += Math.Max(0, a.AmountDue - paid);
        }

        if (totalDueNow <= 0)
        {
            await tx.RollbackAsync(cancellationToken);
            return [];
        }

        var remaining = Math.Min(vm.Amount, totalDueNow);
        var created = new List<ExpenseAllocationPayment>();

        foreach (var a in allocs)
        {
            if (remaining <= 0) break;
            var paid = await GetPaidForAllocationAsync(a.Id, cancellationToken);
            var due = a.AmountDue - paid;
            if (due <= 0) continue;

            var take = Math.Min(due, remaining);
            var entity = new ExpenseAllocationPayment
            {
                ExpenseAllocationId = a.Id,
                Amount = take,
                PaymentDate = vm.PaymentDate,
                Reference = vm.Reference,
                CommonBillId = commonBillId,
                OwnerId = ownerId,
                IdempotencyKey = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? null : vm.IdempotencyKey,
                Gateway = string.IsNullOrWhiteSpace(vm.IdempotencyKey) ? PaymentGateway.None : PaymentGateway.Stripe,
                Status = PaymentStatus.Succeeded
            };
            await context.ExpenseAllocationPayments.AddAsync(entity, cancellationToken);
            created.Add(entity);

            if (take == due) { a.IsPaid = true; a.PaymentDate = vm.PaymentDate; }
            remaining -= take;
        }

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<List<ExpenseAllocationPayment>> RecordPayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var q = context.ExpenseAllocations.Include(a => a.CommonBill).Where(a => a.OwnerId == ownerId);
        if (restrictToBuildingId != null)
            q = q.Where(a => a.CommonBill!.BuildingId == restrictToBuildingId);

        var allocs = await q.ToListAsync(cancellationToken);
        if (allocs.Count == 0) { await tx.RollbackAsync(cancellationToken); return []; }

        var created = new List<ExpenseAllocationPayment>();
        var today = DateTime.Today;

        foreach (var a in allocs.OrderBy(x => x.CommonBill!.BillDate).ThenBy(x => x.Id))
        {
            var paid = await GetPaidForAllocationAsync(a.Id, cancellationToken);
            var due = a.AmountDue - paid;
            if (due <= 0) continue;

            var entity = new ExpenseAllocationPayment
            {
                ExpenseAllocationId = a.Id,
                Amount = due,
                PaymentDate = today,
                Reference = "PayAll",
                CommonBillId = a.CommonBillId,
                OwnerId = ownerId,
                Status = PaymentStatus.Succeeded
            };
            await context.ExpenseAllocationPayments.AddAsync(entity, cancellationToken);
            created.Add(entity);
            a.IsPaid = true;
            a.PaymentDate = today;
        }

        if (created.Count == 0) { await tx.RollbackAsync(cancellationToken); return []; }

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return created;
    }

    public async Task<List<ExpenseAllocationPayment>> RecordFullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var allocs = await GetAllocationsForPayAsync(ownerId, commonBillId, restrictToBuildingId, cancellationToken);
        if (allocs.Count == 0) { await tx.RollbackAsync(cancellationToken); return []; }

        var totalDueNow = 0m;
        foreach (var a in allocs)
        {
            var paid = await GetPaidForAllocationAsync(a.Id, cancellationToken);
            totalDueNow += Math.Max(0, a.AmountDue - paid);
        }
        if (totalDueNow <= 0) { await tx.RollbackAsync(cancellationToken); return []; }

        var created = new List<ExpenseAllocationPayment>();
        var today = DateTime.Today;

        foreach (var a in allocs.OrderBy(x => x.Id))
        {
            var paid = await GetPaidForAllocationAsync(a.Id, cancellationToken);
            var due = a.AmountDue - paid;
            if (due <= 0) continue;

            var entity = new ExpenseAllocationPayment
            {
                ExpenseAllocationId = a.Id,
                Amount = due,
                PaymentDate = today,
                Reference = "FullPay",
                CommonBillId = commonBillId,
                OwnerId = ownerId,
                Status = PaymentStatus.Succeeded
            };
            await context.ExpenseAllocationPayments.AddAsync(entity, cancellationToken);
            created.Add(entity);
            a.IsPaid = true;
            a.PaymentDate = today;
        }

        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);
        return created;
    }
}
