using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Building;
using ApartmentManagementSystem.ViewModels.Flat;

namespace ApartmentManagementSystem.Features.Tenancy.Repositories;

public interface ITenantDirectoryRepository
{
    Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
