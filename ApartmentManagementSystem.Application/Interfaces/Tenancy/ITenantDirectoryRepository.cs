using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Buildings.DTOs;
using ApartmentManagementSystem.Application.Features.Flats.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Tenancy;

public interface ITenantDirectoryRepository
{
    Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
