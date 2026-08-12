using AMS.Application.Features.Administration.DTOs;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace AMS.Application.Features.Administration.Commands;

public record CreateUserCommand(CreateUserViewModel Model, string CreatedByUserId) : IRequest<(bool success, IEnumerable<string> errors)>;

public class CreateUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<CreateUserCommand, (bool success, IEnumerable<string> errors)>
{
    public async Task<(bool success, IEnumerable<string> errors)> Handle(CreateUserCommand request, CancellationToken cancellationToken = default)
    {
        var model = request.Model;
        var isAutoApproved = model.Role != Roles.User;

        var user = new ApplicationUser
        {
            Fullname = model.Fullname,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            BuildingId = model.BuildingId,
            EmailConfirmed = true,
            IsApproved = isAutoApproved,
            ApprovedAt = isAutoApproved ? DateTime.UtcNow : null,
            ApprovedByUserId = isAutoApproved ? request.CreatedByUserId : null,
            CreatedAt = DateTime.UtcNow
        };

        var createResult = await userManager.CreateAsync(user, model.Password);
        if (!createResult.Succeeded)
            return (false, createResult.Errors.Select(e => e.Description));

        var rolesToAssign = model.Role switch
        {
            Roles.Tenant => new[] { Roles.Tenant },
            Roles.Owner => new[] { Roles.Owner },
            Roles.Staff => new[] { Roles.Staff },
            _ => new[] { Roles.User }
        };
        await UserRoleHelper.EnsureOnlyRolesAsync(userManager, user, rolesToAssign);

        return (true, []);
    }
}
