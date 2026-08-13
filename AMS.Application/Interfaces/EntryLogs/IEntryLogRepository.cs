using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.EntryLogs;

public interface IEntryLogRepository
{
    Task<IReadOnlyList<EntryLog>> GetForBuildingAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task<EntryLog?> GetAsync(Guid id, bool includeReferences, CancellationToken cancellationToken = default);
    Task<bool> FlatBelongsToBuildingAsync(Guid flatId, Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Building>> GetBuildingsAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Flat>> GetFlatsAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task AddAsync(EntryLog entry, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    void Remove(EntryLog entry);
}
