using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AMS.Application.Features.Administration.Commands;

public record AssignPresidentCommand(Guid BuildingId, string OwnerUserId, string ApprovedByUserId) : IRequest<(bool success, string message)>;

public class AssignPresidentCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<AssignPresidentCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(AssignPresidentCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(request.OwnerUserId);
        if (user == null) return (false, "Invalid owner.");
        if (user.BuildingId != request.BuildingId) return (false, "Selected owner does not belong to the chosen building.");
        if (!await userManager.IsInRoleAsync(user, Roles.Owner)) return (false, "Selected user is not an Owner.");

        foreach (var r in new[] { Roles.User, Roles.Tenant })
            if (await userManager.IsInRoleAsync(user, r))
                await userManager.RemoveFromRoleAsync(user, r);

        if (!await userManager.IsInRoleAsync(user, Roles.President))
            await userManager.AddToRoleAsync(user, Roles.President);
        if (!await userManager.IsInRoleAsync(user, Roles.Owner))
            await userManager.AddToRoleAsync(user, Roles.Owner);

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByUserId = request.ApprovedByUserId;
        await userManager.UpdateAsync(user);

        return (true, $"Assigned {user.Fullname} as President.");
    }
}
