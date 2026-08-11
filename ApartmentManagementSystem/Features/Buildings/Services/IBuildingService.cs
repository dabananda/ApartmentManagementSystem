using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Buildings.ViewModels;

namespace ApartmentManagementSystem.Features.Buildings.Services;
public interface IBuildingService
{
    Task<BuildingIndexPageViewModel> GetIndexAsync(BuildingIndexFilterViewModel filter, CancellationToken cancellationToken = default);
    Task<Building?> GetAsync(Guid id, bool includeFlats = false, CancellationToken cancellationToken = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default);
    Task CreateAsync(Building building, CancellationToken cancellationToken = default);
    Task UpdateAsync(Building building, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasBlockingRecordsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Building building, CancellationToken cancellationToken = default);
}
