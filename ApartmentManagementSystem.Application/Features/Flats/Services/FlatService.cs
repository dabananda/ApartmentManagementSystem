using ApartmentManagementSystem.Application.Interfaces.Flats;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Flats.DTOs;

namespace ApartmentManagementSystem.Application.Features.Flats.Services;

public sealed class FlatService(IFlatRepository flats) : IFlatService
{
    public Task<IReadOnlyList<Flat>> GetAllWithReferencesAsync(CancellationToken cancellationToken = default) => flats.GetAllWithReferencesAsync(cancellationToken);
    public Task<IReadOnlyList<TenantAssignment>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default) => flats.GetActiveAssignmentsAsync(cancellationToken);
    public Task<Building?> GetBuildingAsync(Guid id, CancellationToken cancellationToken = default) => flats.GetBuildingAsync(id, cancellationToken);
    public Task<IReadOnlyList<Flat>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) => flats.GetForBuildingAsync(buildingId, cancellationToken);
    public Task<Flat?> GetAsync(Guid id, bool includeReferences = false, bool asNoTracking = false, CancellationToken cancellationToken = default) => flats.GetAsync(id, includeReferences, asNoTracking, cancellationToken);
    public Task CreateAsync(Flat flat, CancellationToken cancellationToken = default) => flats.AddAsync(flat, cancellationToken);
    public Task<FlatDeletionCheck> GetDeletionCheckAsync(Guid flatId, CancellationToken cancellationToken = default) => flats.GetDeletionCheckAsync(flatId, cancellationToken);

    public async Task AssignOwnerAsync(Flat flat, string? ownerId, CancellationToken cancellationToken = default)
    {
        flat.OwnerId = ownerId;
        flat.IsOccupied = !string.IsNullOrEmpty(ownerId);
        await flats.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Flat flat, CancellationToken cancellationToken = default)
    {
        flats.Remove(flat);
        await flats.SaveChangesAsync(cancellationToken);
    }
}
