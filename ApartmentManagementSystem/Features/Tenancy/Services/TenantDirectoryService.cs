using ApartmentManagementSystem.Features.Tenancy.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Buildings.ViewModels;
using ApartmentManagementSystem.Features.Flats.ViewModels;

namespace ApartmentManagementSystem.Features.Tenancy.Services;

public sealed class TenantDirectoryService(ITenantDirectoryRepository tenants) : ITenantDirectoryService
{
    public Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default) => tenants.GetFlatAsync(flatId, cancellationToken);
    public Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default) => tenants.GetFlatTenantsAsync(flat, cancellationToken);
    public Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) => tenants.GetBuildingAsync(buildingId, cancellationToken);
    public Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default) => tenants.GetBuildingTenantsAsync(buildingId, cancellationToken);
}
