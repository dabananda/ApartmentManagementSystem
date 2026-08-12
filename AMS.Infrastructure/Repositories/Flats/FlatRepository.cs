using AMS.Application.Interfaces.Flats;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Flats.DTOs;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Flats;

public sealed class FlatRepository(ApplicationDbContext context) : IFlatRepository
{
    public async Task<IReadOnlyList<Flat>> GetAllWithReferencesAsync(CancellationToken cancellationToken = default) =>
        await context.Flats.Include(flat => flat.Owner).Include(flat => flat.Building).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TenantAssignment>> GetActiveAssignmentsAsync(CancellationToken cancellationToken = default) =>
        await context.TenantAssignments.Include(assignment => assignment.TenantUser)
            .Where(assignment => assignment.EndDate == null).ToListAsync(cancellationToken);

    public Task<Building?> GetBuildingAsync(Guid id, CancellationToken cancellationToken = default) =>
        context.Buildings.FindAsync([id], cancellationToken).AsTask();

    public async Task<IReadOnlyList<Flat>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.Flats.Include(flat => flat.Owner).Include(flat => flat.Tenants)
            .Where(flat => flat.BuildingId == buildingId).ToListAsync(cancellationToken);

    public Task<Flat?> GetAsync(Guid id, bool includeReferences = false, bool asNoTracking = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Flat> query = context.Flats;
        if (asNoTracking) query = query.AsNoTracking();
        if (includeReferences) query = query.Include(flat => flat.Building).Include(flat => flat.Owner);
        return query.FirstOrDefaultAsync(flat => flat.Id == id, cancellationToken);
    }

    public async Task AddAsync(Flat flat, CancellationToken cancellationToken = default)
    {
        await context.Flats.AddAsync(flat, cancellationToken);
        await context.SaveChangesAsync(cancellationToken);
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => context.SaveChangesAsync(cancellationToken);
    public void Remove(Flat flat) => context.Flats.Remove(flat);

    public async Task<FlatDeletionCheck> GetDeletionCheckAsync(Guid flatId, CancellationToken cancellationToken = default) => new(
        await context.TenantBills.AnyAsync(bill => bill.FlatId == flatId, cancellationToken),
        await context.Tenants.AnyAsync(tenant => tenant.FlatId == flatId, cancellationToken),
        await context.TenantAssignments.AnyAsync(assignment => assignment.FlatId == flatId && assignment.EndDate == null, cancellationToken),
        await context.EntryLogs.AnyAsync(entry => entry.FlatId == flatId, cancellationToken));
}
