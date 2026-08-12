using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record BulkApproveCommand(string[] Ids, string Role, string ApprovedByUserId, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<int>;

public class BulkApproveCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BulkApproveCommand, int>
{
    public async Task<int> Handle(BulkApproveCommand request, CancellationToken cancellationToken = default)
    {
        if (!UserRoleHelper.IsValidApprovalRole(request.Role)) return 0;

        var users = await userManager.Users
            .Include(u => u.Building)
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        int applied = 0;

        foreach (var user in users)
        {
            if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null && user.BuildingId != request.CallerBuildingId)
                continue;

            var targetIsPresident = await userManager.IsInRoleAsync(user, Roles.President);
            if (targetIsPresident)
            {
                if (!request.CallerIsSuperAdmin && request.Role == Roles.Tenant) continue;
                await UserRoleHelper.HandlePresidentRoleChangeAsync(userManager, user, request.Role, request.CallerIsSuperAdmin);
            }
            else
            {
                foreach (var r in new[] { Roles.User, Roles.Staff, Roles.Owner, Roles.Tenant })
                    if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
                await userManager.AddToRoleAsync(user, request.Role);
            }

            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = request.ApprovedByUserId;
            await userManager.UpdateAsync(user);
            applied++;
        }

        return applied;
    }
}
