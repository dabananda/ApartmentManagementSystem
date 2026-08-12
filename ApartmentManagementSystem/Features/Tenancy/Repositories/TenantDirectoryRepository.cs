using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Buildings.ViewModels;
using ApartmentManagementSystem.Features.Flats.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Tenancy.Repositories;

public sealed class TenantDirectoryRepository(ApplicationDbContext context) : ITenantDirectoryRepository
{
    public Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default) =>
        context.Flats.AsNoTracking().Include(flat => flat.Building).FirstOrDefaultAsync(flat => flat.Id == flatId, cancellationToken);

    public async Task<IReadOnlyList<FlatTenantRow>> GetFlatTenantsAsync(Flat flat, CancellationToken cancellationToken = default)
    {
        var assignmentRows = await context.TenantAssignments.AsNoTracking().Include(assignment => assignment.TenantUser)
            .Where(assignment => assignment.FlatId == flat.Id && assignment.EndDate == null)
            .Select(assignment => new FlatTenantRow { FlatId = assignment.FlatId, FlatNumber = flat.FlatNumber, TenantUserId = assignment.TenantUserId, TenantName = assignment.TenantUser!.Fullname ?? assignment.TenantUser.UserName!, Email = assignment.TenantUser!.Email!, PhoneNumber = assignment.TenantUser!.PhoneNumber, IsActive = true, Source = "Assignment" }).ToListAsync(cancellationToken);
        var assignedUserIds = assignmentRows.Select(row => row.TenantUserId).Where(id => !string.IsNullOrWhiteSpace(id)).ToHashSet();
        var legacyRows = await context.Tenants.AsNoTracking().Where(tenant => tenant.FlatId == flat.Id && (tenant.UserId == null || !assignedUserIds.Contains(tenant.UserId)))
            .Select(tenant => new FlatTenantRow { FlatId = tenant.FlatId, FlatNumber = flat.FlatNumber, LegacyTenantId = tenant.Id, TenantUserId = tenant.UserId, TenantName = tenant.Fullname, Email = tenant.Email, PhoneNumber = tenant.PhoneNumber, IsActive = tenant.IsActive, Source = "Legacy" }).ToListAsync(cancellationToken);
        return assignmentRows.Concat(legacyRows).OrderBy(row => row.TenantName).ToList();
    }

    public Task<Building?> GetBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        context.Buildings.AsNoTracking().FirstOrDefaultAsync(building => building.Id == buildingId, cancellationToken);

    public async Task<IReadOnlyList<BuildingTenantRow>> GetBuildingTenantsAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var assignmentRows = await context.TenantAssignments.AsNoTracking().Include(assignment => assignment.Flat)!.ThenInclude(flat => flat.Owner).Include(assignment => assignment.TenantUser)
            .Where(assignment => assignment.EndDate == null && assignment.Flat!.BuildingId == buildingId)
            .Select(assignment => new BuildingTenantRow { FlatId = assignment.FlatId, FlatNumber = assignment.Flat!.FlatNumber, TenantUserId = assignment.TenantUserId, TenantName = assignment.TenantUser!.Fullname ?? assignment.TenantUser.UserName!, Email = assignment.TenantUser!.Email!, PhoneNumber = assignment.TenantUser!.PhoneNumber, OwnerName = assignment.Flat!.Owner != null ? (assignment.Flat.Owner.Fullname ?? assignment.Flat.Owner.UserName!) : "", IsActive = true, Source = "Assignment" }).ToListAsync(cancellationToken);
        var assignedFlatIds = assignmentRows.Select(row => row.FlatId).ToHashSet();
        var legacyRows = await context.Tenants.AsNoTracking().Include(tenant => tenant.Flat)!.ThenInclude(flat => flat.Owner)
            .Where(tenant => tenant.IsActive && tenant.Flat!.BuildingId == buildingId && !assignedFlatIds.Contains(tenant.FlatId))
            .Select(tenant => new BuildingTenantRow { FlatId = tenant.FlatId, FlatNumber = tenant.Flat!.FlatNumber, TenantUserId = tenant.UserId ?? "", TenantName = tenant.Fullname, Email = tenant.Email, PhoneNumber = tenant.PhoneNumber, OwnerName = tenant.Flat!.Owner != null ? (tenant.Flat.Owner.Fullname ?? tenant.Flat.Owner.UserName!) : "", IsActive = tenant.IsActive, Source = "Legacy" }).ToListAsync(cancellationToken);
        return assignmentRows.Concat(legacyRows).OrderBy(row => row.FlatNumber).ThenBy(row => row.TenantName).ToList();
    }
}
