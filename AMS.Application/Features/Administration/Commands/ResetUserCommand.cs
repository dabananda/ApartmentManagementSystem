using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record ResetUserCommand(string UserId, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<(bool success, string message)>;

public class ResetUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<ResetUserCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(ResetUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .Include(u => u.Building)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null && user.BuildingId != request.CallerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, Roles.President);
        if (targetIsPresident && !request.CallerIsSuperAdmin)
            return (false, "Only SuperAdmin can reset a President.");

        foreach (var r in new[] { Roles.Owner, Roles.Tenant, Roles.President, Roles.User })
            if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
        await userManager.AddToRoleAsync(user, Roles.User);

        user.IsApproved = false;
        user.ApprovedAt = null;
        user.ApprovedByUserId = null;
        await userManager.UpdateAsync(user);

        return (true, $"Reset {user.Fullname} to pending.");
    }
}
