using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.TenantBilling.Services;

public interface ITenantRentService
{
    Task<List<TenantRentListRow>> GetTenantRentListAsync(string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<bool> IsTenantVisibleToOwnerAsync(string tenantUserId, string ownerId, CancellationToken cancellationToken = default);
    Task<TenantBillsPage?> GetTenantBillsPageAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<List<TenantPaymentRecord>> GetTenantPaymentHistoryAsync(string tenantUserId, CancellationToken cancellationToken = default);

    // Receipt fetching
    Task<(TenantPayment? payment, string ownerId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    // Payment workflow
    Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> PayAsync(RecordTenantPaymentVM vm, string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> FullPayAsync(Guid billId, string? restrictToOwnerId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<TenantPayment> payments, string? tenantUserId)> PayAllAsync(string tenantUserId, string? restrictToOwnerId, CancellationToken cancellationToken = default);

    // Bill Generation
    Task<int> EnsureCurrentMonthBillsForTenantAsync(string tenantUserId, CancellationToken cancellationToken = default);
}
