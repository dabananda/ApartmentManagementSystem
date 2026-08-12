using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Features.EntryLogs.Services;

public interface IEntryLogService
{
    Task<IReadOnlyList<EntryLog>> GetForBuildingAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task<EntryLog?> GetAsync(Guid id, bool includeReferences = false, CancellationToken cancellationToken = default);
    Task<bool> FlatBelongsToBuildingAsync(Guid flatId, Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Building>> GetBuildingsAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flat>> GetFlatsAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task CreateAsync(EntryLog entry, CancellationToken cancellationToken = default);
    Task UpdateAsync(EntryLog existing, EntryLog input, CancellationToken cancellationToken = default);
    Task DeleteAsync(EntryLog entry, CancellationToken cancellationToken = default);
}
