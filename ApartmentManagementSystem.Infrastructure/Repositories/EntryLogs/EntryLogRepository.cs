using ApartmentManagementSystem.Application.Interfaces.EntryLogs;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Infrastructure.Repositories.EntryLogs;

public sealed class EntryLogRepository(ApplicationDbContext context) : IEntryLogRepository
{
    public async Task<IReadOnlyList<EntryLog>> GetForBuildingAsync(Guid? buildingId, CancellationToken cancellationToken = default)
    {
        var query = context.EntryLogs.Include(x => x.Building).Include(x => x.Flat).AsQueryable();
        if (buildingId.HasValue) query = query.Where(entry => entry.BuildingId == buildingId.Value);
        return await query.ToListAsync(cancellationToken);
    }

    public Task<EntryLog?> GetAsync(Guid id, bool includeReferences, CancellationToken cancellationToken = default)
    {
        IQueryable<EntryLog> query = context.EntryLogs;
        if (includeReferences) query = query.Include(entry => entry.Building).Include(entry => entry.Flat);
        return query.FirstOrDefaultAsync(entry => entry.Id == id, cancellationToken);
    }

    public Task<bool> FlatBelongsToBuildingAsync(Guid flatId, Guid buildingId, CancellationToken cancellationToken = default) =>
        context.Flats.AnyAsync(flat => flat.Id == flatId && flat.BuildingId == buildingId, cancellationToken);

    public async Task<IReadOnlyList<Building>> GetBuildingsAsync(Guid? buildingId, CancellationToken cancellationToken = default)
    {
        var query = context.Buildings.AsQueryable();
        if (buildingId.HasValue) query = query.Where(building => building.Id == buildingId.Value);
        return await query.OrderBy(building => building.Name).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Flat>> GetFlatsAsync(Guid? buildingId, CancellationToken cancellationToken = default)
    {
        if (!buildingId.HasValue) return [];
        return await context.Flats.Where(flat => flat.BuildingId == buildingId.Value)
            .OrderBy(flat => flat.FlatNumber).ToListAsync(cancellationToken);
    }

    public async Task AddAsync(EntryLog entry, CancellationToken cancellationToken = default)
    {
        await context.EntryLogs.AddAsync(entry, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
    public void Remove(EntryLog entry) => context.EntryLogs.Remove(entry);
}
