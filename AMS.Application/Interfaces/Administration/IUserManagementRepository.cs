using AMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Interfaces.Administration;

public interface IUserManagementRepository
{
    /// <summary>Returns buildings as SelectListItems, optionally restricted to a single building.</summary>
    Task<List<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default);

    /// <summary>Returns a single building as a SelectListItem list (for President users who can only see their building).</summary>
    Task<SelectListItem?> GetBuildingSelectItemAsync(Guid buildingId, CancellationToken cancellationToken = default);

    /// <summary>Returns whether a user has billing history (TenantBills) or active tenant assignments.</summary>
    Task<bool> HasBlockingRecordsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>Returns an IQueryable of users who are either unapproved or belong to a specific role.</summary>
    IQueryable<ApplicationUser> GetUsersByRoleQuery(string roleName);

    /// <summary>Retrieves a mapping of User IDs to their Roles for a given set of user IDs in a single query.</summary>
    Task<Dictionary<string, IList<string>>> GetRolesForUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}
