using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record ChangeRoleCommand(string UserId, string Role, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<(bool success, string message)>;

public class ChangeRoleCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ChangeRoleCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(ChangeRoleCommand request, CancellationToken cancellationToken = default)
    {
        if (!UserRoleHelper.IsValidApprovalRole(request.Role)) return (false, "Invalid role.");

        var user = await userManager.Users
            .Include(u => u.Building)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null && user.BuildingId != request.CallerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, Roles.President);

        if (targetIsPresident)
        {
            if (!request.CallerIsSuperAdmin && request.Role == Roles.Tenant)
                return (false, "A President cannot be assigned the Tenant role.");

            await UserRoleHelper.HandlePresidentRoleChangeAsync(userManager, user, request.Role, request.CallerIsSuperAdmin);
        }
        else
        {
            foreach (var r in new[] { Roles.User, Roles.Staff, Roles.Owner, Roles.Tenant })
                if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
            await userManager.AddToRoleAsync(user, request.Role);
        }

        var displayRole = targetIsPresident && !request.CallerIsSuperAdmin ? $"{Roles.President} + {request.Role}" : request.Role;
        return (true, $"Changed role of {user.Fullname} to {displayRole}.");
    }
}
