using AMS.Application.Features.Buildings.DTOs;
using AMS.Application.Interfaces.Buildings;
using AMS.Domain.Entities;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Buildings;

public sealed class BuildingRepository(ApplicationDbContext context) : IBuildingRepository
{
    public async Task<BuildingIndexPageViewModel> GetIndexAsync(BuildingIndexFilterViewModel filter, CancellationToken cancellationToken = default)
    {
        IQueryable<Building> query = context.Buildings.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var search = filter.Query.Trim().ToLower();
            query = query.Where(building => building.Name.ToLower().Contains(search) || building.Code.ToLower().Contains(search) || (building.Address != null && building.Address.ToLower().Contains(search)));
        }

        var presidentRoleId = await context.Roles.Where(role => role.Name == "President").Select(role => role.Id).FirstOrDefaultAsync(cancellationToken);
        var buildingIdsWithPresident = string.IsNullOrEmpty(presidentRoleId) ? [] : await (
            from user in context.Users
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            where userRole.RoleId == presidentRoleId && user.BuildingId != null
            select user.BuildingId!.Value).Distinct().ToListAsync(cancellationToken);
        if (filter.HasPresident == true) query = query.Where(building => buildingIdsWithPresident.Contains(building.Id));
        else if (filter.HasPresident == false) query = query.Where(building => !buildingIdsWithPresident.Contains(building.Id));

        var total = await query.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var page = Math.Max(1, filter.Page);
        var buildings = await query.OrderBy(building => building.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var buildingIds = buildings.Select(building => building.Id).ToList();

        var flatCounts = await context.Flats.Where(flat => buildingIds.Contains(flat.BuildingId)).GroupBy(flat => flat.BuildingId)
            .Select(group => new { group.Key, Count = group.Count() }).ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
        var users = await context.Users.Where(user => user.BuildingId != null && buildingIds.Contains(user.BuildingId.Value))
            .Select(user => new { user.Id, user.BuildingId, user.Fullname, user.Email }).ToListAsync(cancellationToken);
        var userIds = users.Select(user => user.Id).ToList();
        var userBuildings = users.ToDictionary(user => user.Id, user => user.BuildingId!.Value);
        var names = users.ToDictionary(user => user.Id, user => string.IsNullOrWhiteSpace(user.Fullname) ? user.Email ?? "-" : user.Fullname);
        var roles = await context.Roles.Where(role => role.Name == "Tenant" || role.Name == "Owner" || role.Name == "President").Select(role => new { role.Id, role.Name }).ToListAsync(cancellationToken);
        var tenantRoleId = roles.FirstOrDefault(role => role.Name == "Tenant")?.Id;
        var ownerRoleId = roles.FirstOrDefault(role => role.Name == "Owner")?.Id;
        var presidentId = roles.FirstOrDefault(role => role.Name == "President")?.Id;
        var userRoles = await context.UserRoles.Where(userRole => userIds.Contains(userRole.UserId) && (userRole.RoleId == tenantRoleId || userRole.RoleId == ownerRoleId || userRole.RoleId == presidentId)).ToListAsync(cancellationToken);
        var tenantCounts = userRoles.Where(role => role.RoleId == tenantRoleId).GroupBy(role => userBuildings[role.UserId]).ToDictionary(group => group.Key, group => group.Count());
        var ownerCounts = userRoles.Where(role => role.RoleId == ownerRoleId).GroupBy(role => userBuildings[role.UserId]).ToDictionary(group => group.Key, group => group.Count());
        var presidents = userRoles.Where(role => role.RoleId == presidentId).GroupBy(role => userBuildings[role.UserId]).ToDictionary(group => group.Key, group => names[group.First().UserId]);

        return new BuildingIndexPageViewModel { Filter = filter, Total = total, Buildings = buildings.Select(building => new BuildingListItemViewModel { Id = building.Id, Name = building.Name, Code = building.Code, Address = building.Address, FlatsCount = flatCounts.GetValueOrDefault(building.Id), TenantsCount = tenantCounts.GetValueOrDefault(building.Id), OwnersCount = ownerCounts.GetValueOrDefault(building.Id), PresidentName = presidents.GetValueOrDefault(building.Id, "-") }).ToList() };
    }

    public Task<Building?> GetAsync(Guid id, bool includeFlats = false, CancellationToken cancellationToken = default)
    {
        IQueryable<Building> query = context.Buildings;
        if (includeFlats) query = query.Include(building => building.Flats);
        return query.FirstOrDefaultAsync(building => building.Id == id, cancellationToken);
    }
    public Task<bool> CodeExistsAsync(string code, CancellationToken cancellationToken = default) => context.Buildings.AnyAsync(building => building.Code == code, cancellationToken);
    public async Task AddAsync(Building building, CancellationToken cancellationToken = default) { await context.AddAsync(building, cancellationToken); await context.SaveChangesAsync(cancellationToken); }
    public async Task UpdateAsync(Building building, CancellationToken cancellationToken = default) { context.Update(building); await context.SaveChangesAsync(cancellationToken); }
    public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default) => context.Buildings.AnyAsync(building => building.Id == id, cancellationToken);
    public async Task<bool> HasBlockingRecordsAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var flatIds = await context.Flats.Where(flat => flat.BuildingId == buildingId).Select(flat => flat.Id).ToListAsync(cancellationToken);
        return await context.TenantBills.AnyAsync(bill => flatIds.Contains(bill.FlatId), cancellationToken) || await context.Tenants.AnyAsync(tenant => flatIds.Contains(tenant.FlatId), cancellationToken) || await context.TenantAssignments.AnyAsync(assignment => flatIds.Contains(assignment.FlatId) && assignment.EndDate == null, cancellationToken) || await context.EntryLogs.AnyAsync(entry => flatIds.Contains(entry.FlatId), cancellationToken);
    }
    public async Task DeleteAsync(Building building, CancellationToken cancellationToken = default) { context.Buildings.Remove(building); await context.SaveChangesAsync(cancellationToken); }
}
