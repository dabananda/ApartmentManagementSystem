using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Flats.DTOs;

namespace ApartmentManagementSystem.Application.Features.Flats.Services;

public interface IFlatService
{
    Task<IReadOnlyList<Flat>> GetAllWithReferencesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantAssignment>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flat>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<Flat?> GetAsync(Guid id, bool includeReferences = false, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task CreateAsync(Flat flat, CancellationToken cancellationToken = default);
    Task AssignOwnerAsync(Flat flat, string? ownerId, CancellationToken cancellationToken = default);
    Task<FlatDeletionCheck> GetDeletionCheckAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task DeleteAsync(Flat flat, CancellationToken cancellationToken = default);
}
