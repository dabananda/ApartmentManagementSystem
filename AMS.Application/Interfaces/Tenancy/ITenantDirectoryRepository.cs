using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Buildings.DTOs;
using AMS.Application.Features.Flats.DTOs;

namespace AMS.Application.Interfaces.Tenancy;

public interface ITenantDirectoryRepository
{
    Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
