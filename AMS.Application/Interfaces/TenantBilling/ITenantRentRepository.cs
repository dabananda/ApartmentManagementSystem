using AMS.Application.Features.Tenancy.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.TenantBilling;

public interface ITenantRentRepository
{
    Task<List<TenantRentListRow>> GetTenantRentListAsync(string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<bool> IsTenantVisibleToOwnerAsync(string tenantUserId, string ownerId, CancellationToken cancellationToken = default);
    Task<TenantBillsPage?> GetTenantBillsPageAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<List<TenantPaymentRecord>> GetTenantPaymentHistoryAsync(string tenantUserId, CancellationToken cancellationToken = default);

    Task<(TenantPayment? payment, string ownerId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    Task<bool> IdempotencyKeyExistsAsync(string key, CancellationToken cancellationToken = default);
    Task<(List<TenantPayment> payments, string? tenantUserId)> RecordPayAsync(RecordTenantPaymentVM vm, string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<(List<TenantPayment> payments, string? tenantUserId)> RecordFullPayAsync(Guid billId, string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<(List<TenantPayment> payments, string? tenantUserId)> RecordPayAllAsync(string tenantUserId, string? restrictToOwnerId, CancellationToken cancellationToken = default);

    Task<int> EnsureCurrentMonthBillsForTenantAsync(string tenantUserId, CancellationToken cancellationToken = default);
}
