using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record BlockUserCommand(string UserId, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<(bool success, string message)>;

public class BlockUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BlockUserCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(BlockUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .Include(u => u.Building)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null && user.BuildingId != request.CallerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, Roles.President);
        var targetIsSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);

        if (targetIsSuperAdmin) return (false, "Cannot block a SuperAdmin.");
        if (targetIsPresident && !request.CallerIsSuperAdmin) return (false, "Only SuperAdmin can block a President.");

        user.LockoutEnd = DateTimeOffset.MaxValue;
        await userManager.UpdateAsync(user);

        return (true, $"Blocked {user.Fullname}.");
    }
}
