using ApartmentManagementSystem.Features.Buildings.Repositories;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Building;

namespace ApartmentManagementSystem.Features.Buildings.Services;
public sealed class BuildingService(IBuildingRepository buildings) : IBuildingService
{
    public Task<BuildingIndexPageViewModel> GetIndexAsync(BuildingIndexFilterViewModel filter, CancellationToken cancellationToken = default) => buildings.GetIndexAsync(filter, cancellationToken);
    public Task<Building?> GetAsync(Guid id, bool includeFlats = false, CancellationToken cancellationToken = default) => buildings.GetAsync(id, includeFlats, cancellationToken);
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) => buildings.CodeExistsAsync(code, cancellationToken);
    public Task CreateAsync(Building building, CancellationToken cancellationToken = default) => buildings.AddAsync(building, cancellationToken);
    public Task UpdateAsync(Building building, CancellationToken cancellationToken = default) => buildings.UpdateAsync(building, cancellationToken);
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => buildings.ExistsAsync(id, cancellationToken);
    public Task<bool> HasBlockingRecordsAsync(Guid buildingId, CancellationToken cancellationToken = default) => buildings.HasBlockingRecordsAsync(buildingId, cancellationToken);
    public Task DeleteAsync(Building building, CancellationToken cancellationToken = default) => buildings.DeleteAsync(building, cancellationToken);
}
