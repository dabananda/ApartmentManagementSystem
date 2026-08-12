using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration.Repositories;

public sealed class UserManagementRepository(ApplicationDbContext context) : IUserManagementRepository
{
    public async Task<List<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default)
    {
        var q = context.Buildings.AsNoTracking().OrderBy(b => b.Name);
        if (restrictToBuildingId.HasValue)
            return await q
                .Where(b => b.Id == restrictToBuildingId.Value)
                .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
                .ToListAsync(cancellationToken);

        return await q
            .Select(b => new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" })
            .ToListAsync(cancellationToken);
    }

    public async Task<SelectListItem?> GetBuildingSelectItemAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var b = await context.Buildings.AsNoTracking()
            .Where(x => x.Id == buildingId)
            .Select(x => new { x.Id, x.Name, x.Code })
            .FirstOrDefaultAsync(cancellationToken);
        return b == null ? null : new SelectListItem { Value = b.Id.ToString(), Text = $"{b.Name} ({b.Code})" };
    }

    public async Task<bool> HasBlockingRecordsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var hasBills = await context.TenantBills.AnyAsync(b => b.TenantUserId == userId, cancellationToken);
        if (hasBills) return true;
        return await context.TenantAssignments.AnyAsync(a => a.TenantUserId == userId && a.EndDate == null, cancellationToken);
    }

    public IQueryable<ApplicationUser> GetUsersByRoleQuery(string roleName)
    {
        var query = from u in context.Users
                    join ur in context.UserRoles on u.Id equals ur.UserId
                    join r in context.Roles on ur.RoleId equals r.Id
                    where r.Name == roleName
                    select u;
                    
        return query;
    }

    public async Task<Dictionary<string, IList<string>>> GetRolesForUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default)
    {
        var query = from ur in context.UserRoles
                    join r in context.Roles on ur.RoleId equals r.Id
                    where userIds.Contains(ur.UserId)
                    select new { ur.UserId, r.Name };

        var roleData = await query.AsNoTracking().ToListAsync(cancellationToken);
        
        return roleData
            .GroupBy(x => x.UserId)
            .ToDictionary(
                g => g.Key,
                g => (IList<string>)g.Select(x => x.Name!).ToList()
            );
    }
}
