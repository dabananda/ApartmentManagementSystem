using ApartmentManagementSystem.Features.TenantBilling.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;

namespace ApartmentManagementSystem.Features.TenantBilling.Services;

public sealed class TenantRentService(ITenantRentRepository repository) : ITenantRentService
{
    public Task<List<TenantRentListRow>> GetTenantRentListAsync(string? restrictToOwnerId, CancellationToken cancellationToken = default) =>
        repository.GetTenantRentListAsync(restrictToOwnerId, cancellationToken);

    public Task<bool> IsTenantVisibleToOwnerAsync(string tenantUserId, string ownerId, CancellationToken cancellationToken = default) =>
        repository.IsTenantVisibleToOwnerAsync(tenantUserId, ownerId, cancellationToken);

    public Task<TenantBillsPage?> GetTenantBillsPageAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetTenantBillsPageAsync(tenantUserId, cancellationToken);

    public Task<List<TenantPaymentRecord>> GetTenantPaymentHistoryAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetTenantPaymentHistoryAsync(tenantUserId, cancellationToken);

    public Task<(TenantPayment? payment, string ownerId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default) =>
        repository.GetReceiptDataAsync(paymentId, cancellationToken);

    public async Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> PayAsync(RecordTenantPaymentVM vm, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(vm.IdempotencyKey))
        {
            var exists = await repository.IdempotencyKeyExistsAsync(vm.IdempotencyKey, cancellationToken);
            if (exists) return (true, "Payment recorded.", [], null); // No easy tenant ID, let controller fallback to List
        }

        var (created, tenantUserId) = await repository.RecordPayAsync(vm, restrictToOwnerId, cancellationToken);
        if (created.Count == 0) return (false, "Nothing to pay or no due on this bill.", [], tenantUserId);
        
        var take = created.Sum(p => p.Amount);
        var message = take < vm.Amount
            ? $"Payment recorded (clamped to {take:C} to avoid overpay)."
            : "Payment recorded.";
        
        return (true, message, created, tenantUserId);
    }

    public async Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> FullPayAsync(Guid billId, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        var (created, tenantUserId) = await repository.RecordFullPayAsync(billId, restrictToOwnerId, cancellationToken);
        if (created.Count == 0) return (false, "No due on this bill or bill not found.", [], tenantUserId);
        
        return (true, "Bill fully paid.", created, tenantUserId);
    }

    public async Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> PayAllAsync(string tenantUserId, string? restrictToOwnerId, CancellationToken cancellationToken = default)
    {
        var (created, returnedTenantId) = await repository.RecordPayAllAsync(tenantUserId, restrictToOwnerId, cancellationToken);
        if (created.Count == 0) return (false, "Nothing due to pay or no bills found.", [], tenantUserId);

        return (true, "All outstanding dues paid.", created, tenantUserId);
    }

    public Task<int> EnsureCurrentMonthBillsForTenantAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.EnsureCurrentMonthBillsForTenantAsync(tenantUserId, cancellationToken);
}
