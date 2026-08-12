using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record BulkDeleteCommand(string[] Ids) : IRequest<int>;

public class BulkDeleteCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<BulkDeleteCommand, int>
{
    public async Task<int> Handle(BulkDeleteCommand request, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users
            .Where(u => request.Ids.Contains(u.Id))
            .ToListAsync(cancellationToken);

        int deleted = 0;

        foreach (var user in users)
        {
            var targetIsSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);
            if (targetIsSuperAdmin) continue;

            await userManager.DeleteAsync(user);
            deleted++;
        }

        return deleted;
    }
}
