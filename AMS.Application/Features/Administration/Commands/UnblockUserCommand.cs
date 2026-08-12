using AMS.Application.Mediator;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AMS.Application.Features.Administration.Commands;

public record UnblockUserCommand(string UserId, Guid? CallerBuildingId) : IRequest<(bool success, string message)>;

public class UnblockUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UnblockUserCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(UnblockUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.UserId);
        if (user == null) return (false, "User not found.");

        if (request.CallerBuildingId.HasValue && user.BuildingId != request.CallerBuildingId)
            return (false, "Forbidden.");

        user.LockoutEnd = null;
        await userManager.UpdateAsync(user);

        return (true, $"Unblocked {user.Fullname}.");
    }
}
