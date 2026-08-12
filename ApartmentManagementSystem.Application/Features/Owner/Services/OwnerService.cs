using ApartmentManagementSystem.Application.Interfaces.Owner;
using ApartmentManagementSystem.Application.Features.Owner.DTOs;

namespace ApartmentManagementSystem.Application.Features.Owner.Services;

public sealed class OwnerService(IOwnerRepository repository) : IOwnerService
{
    public async Task<OwnerDashboardVM> GetDashboardAsync(string ownerId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.Today;
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var data = await repository.GetDashboardDataAsync(ownerId, monthStart, cancellationToken);

        return new OwnerDashboardVM
        {
            FlatsOwnedCount = data.FlatsOwnedCount,
            FlatsOccupiedCount = data.FlatsOccupiedCount,
            RentTotalBilled = data.RentTotalBilled,
            RentTotalPaid = data.RentTotalPaid,
            RentPaidThisMonth = data.RentPaidThisMonth,
            CommonTotalBilled = data.CommonTotalBilled,
            CommonTotalPaid = data.CommonTotalPaid,
            Tenants = data.Tenants.ToList(),
            RecentRent = data.RecentRent.ToList(),
            RecentCommon = data.RecentCommon.ToList()
        };
    }

    public Task<IReadOnlyList<OwnerOwnedFlatRow>> GetOwnedFlatsAsync(string ownerId, CancellationToken cancellationToken = default) =>
        repository.GetOwnedFlatsAsync(ownerId, cancellationToken);

    public Task<OwnerBillsPage?> GetCommonBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default) =>
        repository.GetCommonBillsPageAsync(ownerId, restrictToBuildingId, cancellationToken);
}
