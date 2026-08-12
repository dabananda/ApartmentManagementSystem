using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AMS.Application.Features.Administration.Commands;

public record ApproveUserCommand(string UserId, string Role, string ApprovedByUserId, bool CallerIsSuperAdmin, Guid? CallerBuildingId) : IRequest<(bool success, string message)>;

public class ApproveUserCommandHandler(UserManager<ApplicationUser> userManager, IEmailSender email, ILogger<ApproveUserCommandHandler> logger)
    : IRequestHandler<ApproveUserCommand, (bool success, string message)>
{
    public async Task<(bool success, string message)> Handle(ApproveUserCommand request, CancellationToken cancellationToken = default)
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

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByUserId = request.ApprovedByUserId;
        await userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var roleText = targetIsPresident && !request.CallerIsSuperAdmin ? $"{Roles.President} + {request.Role}" : request.Role;
                await email.SendEmailAsync(user.Email,
                    "Your account has been approved",
                    $"<p>Hi {user.Fullname},</p><p>Your role is now <strong>{roleText}</strong>. You can log in now.</p>");
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to send approval email to {Email}", user.Email);
            }
        }

        var displayRole = targetIsPresident && !request.CallerIsSuperAdmin ? $"{Roles.President} + {request.Role}" : request.Role;
        return (true, $"Approved {user.Fullname} as {displayRole}.");
    }
}
