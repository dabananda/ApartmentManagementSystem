using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Buildings.DTOs;
using AMS.Application.Features.Flats.DTOs;

namespace AMS.Application.Features.Tenancy.Services;

public sealed class TenantDirectoryService(ITenantDirectoryRepository tenants) : ITenantDirectoryService
{
    public Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default) => tenants.GetFlatAsync(flatId, cancellationToken);
    public Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default) => tenants.GetFlatTenantsAsync(flat, cancellationToken);
    public Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) => tenants.GetBuildingAsync(buildingId, cancellationToken);
    public Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default) => tenants.GetBuildingTenantsAsync(buildingId, cancellationToken);
}
