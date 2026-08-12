using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Owner.DTOs;

namespace AMS.Application.Interfaces.Owner;

public interface IOwnerRepository
{
    Task<OwnerDashboardData> GetDashboardDataAsync(string ownerId, DateTime monthStart, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerOwnedFlatRow>> GetOwnedFlatsAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetCommonBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}

public sealed record OwnerDashboardData(
    int FlatsOwnedCount,
    int FlatsOccupiedCount,
    decimal RentTotalBilled,
    decimal RentTotalPaid,
    decimal RentPaidThisMonth,
    decimal CommonTotalBilled,
    decimal CommonTotalPaid,
    IReadOnlyList<OwnerTenantRow> Tenants,
    IReadOnlyList<OwnerRecentRentPaymentRow> RecentRent,
    IReadOnlyList<OwnerRecentCommonPaymentRow> RecentCommon);
