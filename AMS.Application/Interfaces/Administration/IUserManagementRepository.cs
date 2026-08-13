using AMS.Domain.Entities;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Interfaces.Administration;

public interface IUserManagementRepository
{
    Task<IEnumerable<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default);

    Task<SelectListItem?> GetBuildingSelectItemAsync(Guid buildingId, CancellationToken cancellationToken = default);

    Task<bool> HasBlockingRecordsAsync(string userId, CancellationToken cancellationToken = default);

    IQueryable<ApplicationUser> GetUsersByRoleQuery(string roleName);

    Task<Dictionary<string, IList<string>>> GetRolesForUsersAsync(IEnumerable<string> userIds, CancellationToken cancellationToken = default);
}

