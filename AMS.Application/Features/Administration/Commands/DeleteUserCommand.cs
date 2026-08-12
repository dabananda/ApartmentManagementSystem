using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record DeleteUserCommand(string UserId) : IRequest<(bool success, string message)>;

public class DeleteUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<DeleteUserCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(DeleteUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .Include(u => u.Building)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);
        if (user == null) return (false, "User not found.");

        var targetIsSuperAdmin = await userManager.IsInRoleAsync(user, Roles.SuperAdmin);
        if (targetIsSuperAdmin) return (false, "Cannot delete a SuperAdmin.");

        await userManager.DeleteAsync(user);

        return (true, $"Deleted {user.Fullname}.");
    }
}
