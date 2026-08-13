using AMS.Application.Features.Administration.DTOs;
using AMS.Application.Mediator;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace AMS.Application.Features.Administration.Commands;

public record UpdateUserCommand(EditUserViewModel Model, bool CallerIsSuperAdmin) : IRequest<(bool success, IEnumerable<string> errors)>;

public class UpdateUserCommandHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<UpdateUserCommand, (bool success, IEnumerable<string> errors)>
{
    public async Task<(bool success, IEnumerable<string> errors)> Handle(UpdateUserCommand request, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users
            .Include(u => u.Building)
            .FirstOrDefaultAsync(u => u.Id == request.Model.Id, cancellationToken);

        if (user == null) return (false, ["User not found."]);

        user.Fullname = request.Model.Fullname?.Trim() ?? user.Fullname;
        user.PhoneNumber = string.IsNullOrWhiteSpace(request.Model.PhoneNumber) ? null : request.Model.PhoneNumber.Trim();

        if (request.CallerIsSuperAdmin)
            user.BuildingId = request.Model.BuildingId;

        var res = await userManager.UpdateAsync(user);
        return res.Succeeded ? (true, []) : (false, res.Errors.Select(e => e.Description));
    }
}
