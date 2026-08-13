using AMS.Application.Features.Administration.DTOs;
using AMS.Application.Interfaces.Administration;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Queries;

public record GetUsersPageQuery(ManageUsersFilterViewModel Filter, Guid? CallerBuildingId, bool CallerIsSuperAdmin) : IRequest<ManageUsersPageViewModel>;

public class GetUsersPageQueryHandler(UserManager<ApplicationUser> userManager, IUserManagementRepository repository)
    : IRequestHandler<GetUsersPageQuery, ManageUsersPageViewModel>
{
    public async Task<ManageUsersPageViewModel> Handle(GetUsersPageQuery request, CancellationToken cancellationToken = default)
    {
        var filter = request.Filter;
        var pendingRolesQuery = repository.GetUsersByRoleQuery(Roles.User);

        IQueryable<ApplicationUser> q = userManager.Users
            .Include(u => u.Building)
            .Where(u => !pendingRolesQuery.Contains(u) && u.IsApproved);

        if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null)
            filter.BuildingId ??= request.CallerBuildingId;

        if (filter.BuildingId != null)
            q = q.Where(u => u.BuildingId == filter.BuildingId);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim().ToLower();
            q = q.Where(u =>
                (u.Fullname ?? "").ToLower().Contains(term) ||
                (u.Email ?? "").ToLower().Contains(term) ||
                (u.PhoneNumber ?? "").ToLower().Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var page = Math.Max(1, filter.Page);
        var users = await q
            .OrderBy(u => u.Fullname)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.Id).ToList();
        var roleMap = await repository.GetRolesForUsersAsync(userIds, cancellationToken);
        foreach (var u in users)
        {
            if (!roleMap.ContainsKey(u.Id)) roleMap[u.Id] = new List<string>();
        }

        var buildings = request.CallerIsSuperAdmin
            ? await repository.GetBuildingSelectItemsAsync(cancellationToken: cancellationToken)
            : request.CallerBuildingId != null
                ? [await repository.GetBuildingSelectItemAsync(request.CallerBuildingId.Value, cancellationToken) ?? new SelectListItem()]
                : new List<SelectListItem>();

        return new ManageUsersPageViewModel
        {
            Filter = filter,
            Buildings = buildings,
            Total = total,
            Users = users.Select(u => new UserRowViewModel
            {
                Id = u.Id,
                Fullname = u.Fullname,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                EmailConfirmed = u.EmailConfirmed,
                IsApproved = u.IsApproved,
                IsLockedOut = u.LockoutEnd > DateTimeOffset.UtcNow,
                IsPresident = roleMap[u.Id].Contains(Roles.President),
                CreatedAt = u.CreatedAt,
                BuildingId = u.BuildingId,
                BuildingName = u.Building?.Name,
                Roles = roleMap[u.Id].ToList()
            }).ToList()
        };
    }
}
