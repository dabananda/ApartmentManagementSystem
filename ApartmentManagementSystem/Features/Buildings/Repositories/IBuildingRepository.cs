using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Building;

namespace ApartmentManagementSystem.Features.Buildings.Repositories;

public interface IBuildingRepository
{
    Task<BuildingIndexPageViewModel> GetIndexAsync(BuildingIndexFilterViewModel filter, CancellationToken cancellationToken = default);
    Task<Building?> GetAsync(Guid id, bool includeFlats = false, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task AddAsync(Building building, CancellationToken cancellationToken = default);
    Task UpdateAsync(Building building, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasBlockingRecordsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Building building, CancellationToken cancellationToken = default);
}
