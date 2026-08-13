using System.Data;
using AMS.Application.Interfaces.Payments;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Payments;

public sealed class PaymentRepository(ApplicationDbContext context) : IPaymentRepository
{
    public Task<TenantBill?> GetTenantBillForCheckoutAsync(Guid billId, string tenantUserId, CancellationToken cancellationToken = default) =>
        context.TenantBills
            .AsNoTracking()
            .Include(b => b.Flat).ThenInclude(f => f!.Building)
            .FirstOrDefaultAsync(b => b.Id == billId && b.TenantUserId == tenantUserId, cancellationToken);

    public async Task<decimal> GetPaidAmountForTenantBillAsync(Guid billId, CancellationToken cancellationToken = default) =>
        await context.TenantPayments
            .Where(p => p.TenantBillId == billId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

    public Task<ExpenseAllocation?> GetExpenseAllocationForCheckoutAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default) =>
        context.ExpenseAllocations
            .AsNoTracking()
            .Include(a => a.CommonBill).ThenInclude(cb => cb!.Building)
            .Where(a => a.CommonBillId == commonBillId && a.OwnerId == ownerId)
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<decimal> GetPaidAmountForAllocationAsync(Guid commonBillId, string ownerId, CancellationToken cancellationToken = default) =>
        await context.ExpenseAllocationPayments
            .Where(p => p.CommonBillId == commonBillId && p.OwnerId == ownerId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

    public async Task<(bool success, TenantPayment? payment)> ProcessTenantPaymentFromWebhookAsync(Guid billId, string tenantUserId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var exists = await context.TenantPayments.AnyAsync(p => p.IdempotencyKey == paymentRef, cancellationToken);
        if (exists) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var bill = await context.TenantBills
            .Include(b => b.Flat)
            .FirstOrDefaultAsync(b => b.Id == billId && b.TenantUserId == tenantUserId, cancellationToken);
        if (bill == null) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var paid = await context.TenantPayments
            .Where(p => p.TenantBillId == bill.Id && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var dueNow = bill.Amount - paid;
        var take = Math.Min(amountReceived, dueNow);
        if (take <= 0m) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var entity = new TenantPayment
        {
            TenantBillId = bill.Id,
            Amount = take,
            PaymentDate = DateTime.Today,
            Reference = $"Stripe {paymentRef}",
            IdempotencyKey = paymentRef,
            ExternalRef = paymentRef,
            Gateway = PaymentGateway.Stripe,
            Status = PaymentStatus.Succeeded
        };

        await context.TenantPayments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (true, entity);
    }

    public async Task<(bool success, ExpenseAllocationPayment? payment)> ProcessOwnerPaymentFromWebhookAsync(Guid commonBillId, string ownerId, decimal amountReceived, string paymentRef, CancellationToken cancellationToken = default)
    {
        await using var tx = await context.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);

        var exists = await context.ExpenseAllocationPayments.AnyAsync(p => p.IdempotencyKey == paymentRef, cancellationToken);
        if (exists) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var alloc = await context.ExpenseAllocations
            .Include(a => a.CommonBill).ThenInclude(cb => cb!.Building)
            .FirstOrDefaultAsync(a => a.CommonBillId == commonBillId && a.OwnerId == ownerId, cancellationToken);
        if (alloc == null) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var paid = await context.ExpenseAllocationPayments
            .Where(p => p.CommonBillId == commonBillId && p.OwnerId == ownerId && p.Status == PaymentStatus.Succeeded)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var dueNow = alloc.AmountDue - paid;
        var take = Math.Min(amountReceived, dueNow);
        if (take <= 0m) { await tx.RollbackAsync(cancellationToken); return (false, null); }

        var entity = new ExpenseAllocationPayment
        {
            ExpenseAllocationId = alloc.Id,
            CommonBillId = commonBillId,
            OwnerId = ownerId,
            Amount = take,
            PaymentDate = DateTime.Today,
            Reference = $"Stripe {paymentRef}",
            IdempotencyKey = paymentRef,
            ExternalRef = paymentRef,
            Gateway = PaymentGateway.Stripe,
            Status = PaymentStatus.Succeeded,
            CreatedAtUtc = DateTime.UtcNow
        };

        await context.ExpenseAllocationPayments.AddAsync(entity, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        return (true, entity);
    }
}
