using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Buildings.ViewModels;
using ApartmentManagementSystem.Features.Flats.ViewModels;

namespace ApartmentManagementSystem.Features.Tenancy.Services;

public interface ITenantDirectoryService
{
    Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
