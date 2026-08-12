using AMS.Application.Interfaces.EntryLogs;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.EntryLogs.Services;

public sealed class EntryLogService(IEntryLogRepository entries) : IEntryLogService
{
    public Task<IReadOnlyList<EntryLog>> GetForBuildingAsync(Guid? buildingId, CancellationToken cancellationToken = default) => entries.GetForBuildingAsync(buildingId, cancellationToken);
    public Task<EntryLog?> GetAsync(Guid id, bool includeReferences = false, CancellationToken cancellationToken = default) => entries.GetAsync(id, includeReferences, cancellationToken);
    public Task<bool> FlatBelongsToBuildingAsync(Guid flatId, Guid buildingId, CancellationToken cancellationToken = default) => entries.FlatBelongsToBuildingAsync(flatId, buildingId, cancellationToken);
    public Task<IReadOnlyList<Building>> GetBuildingsAsync(Guid? buildingId, CancellationToken cancellationToken = default) => entries.GetBuildingsAsync(buildingId, cancellationToken);
    public Task<IReadOnlyList<Flat>> GetFlatsAsync(Guid? buildingId, CancellationToken cancellationToken = default) => entries.GetFlatsAsync(buildingId, cancellationToken);

    public Task CreateAsync(EntryLog entry, CancellationToken cancellationToken = default)
    {
        entry.Id = Guid.NewGuid();
        return entries.AddAsync(entry, cancellationToken);
    }

    public async Task UpdateAsync(EntryLog existing, EntryLog input, CancellationToken cancellationToken = default)
    {
        existing.Fullname = input.Fullname;
        existing.PhoneNumber = input.PhoneNumber;
        existing.BuildingId = input.BuildingId;
        existing.FlatId = input.FlatId;
        existing.EntryType = input.EntryType;
        existing.NumberOfPerson = input.NumberOfPerson;
        existing.Purpose = input.Purpose;
        existing.EntryTime = input.EntryTime;
        existing.ExitTime = input.ExitTime;
        await entries.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(EntryLog entry, CancellationToken cancellationToken = default)
    {
        entries.Remove(entry);
        await entries.SaveChangesAsync(cancellationToken);
    }
}
