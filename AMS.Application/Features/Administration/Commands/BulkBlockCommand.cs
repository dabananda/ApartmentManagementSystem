using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record BulkBlockCommand(string[] Ids, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<int>;

public class BulkBlockCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BulkBlockCommand, int>
{
    public async Task<int> Handle(BulkBlockCommand request, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .Include(u => u.Building)
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        int blocked = 0;

        foreach (var user in users)
        {
            if (!request.CallerIsSuperAdmin && request.CallerBuildingId != null && user.BuildingId != request.CallerBuildingId)
                continue;

            var targetIsSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);
            if (targetIsSuperAdmin) continue;

            var targetIsPresident = await userManager.IsInRoleAsync(user, Roles.President);
            if (targetIsPresident && !request.CallerIsSuperAdmin) continue;

            user.LockoutEnd = DateTimeOffset.MaxValue;
            await userManager.UpdateAsync(user);
            blocked++;
        }

        return blocked;
    }
}
