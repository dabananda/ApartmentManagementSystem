using AMS.Application.Mediator;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record BulkUnblockCommand(string[] Ids, Guid? CallerBuildingId) : IRequest<int>;

public class BulkUnblockCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BulkUnblockCommand, int>
{
    public async Task<int> Handle(BulkUnblockCommand request, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        int unblocked = 0;

        foreach (var user in users)
        {
            if (request.CallerBuildingId.HasValue && user.BuildingId != request.CallerBuildingId)
                continue;

            user.LockoutEnd = null;
            await userManager.UpdateAsync(user);
            unblocked++;
        }

        return unblocked;
    }
}
