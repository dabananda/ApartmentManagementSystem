using AMS.Application.Features.Flats.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.Flats;

public interface IFlatRepository
{
    Task<IReadOnlyList<Flat>> GetAllWithReferencesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<TenantAssignment>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default);
    Task<Building?> GetBuildingAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flat>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<Flat?> GetAsync(Guid id, bool includeReferences = false, bool asNoTracking = false, CancellationToken cancellationToken = default);
    Task AddAsync(Flat flat, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<FlatDeletionCheck> GetDeletionCheckAsync(Guid flatId, CancellationToken cancellationToken = default);
    void Remove(Flat flat);
}
