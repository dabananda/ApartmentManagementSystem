using ApartmentManagementSystem.Features.Owner.ViewModels;

namespace ApartmentManagementSystem.Features.Owner.Services;

public interface IOwnerService
{
    Task<OwnerDashboardVM> GetDashboardAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OwnerOwnedFlatRow>> GetOwnedFlatsAsync(string ownerId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetCommonBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}
