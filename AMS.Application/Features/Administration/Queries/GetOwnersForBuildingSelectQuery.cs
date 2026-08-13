using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.Queries;

public record GetOwnersForBuildingSelectQuery(Guid BuildingId) : IRequest<IEnumerable<SelectListItem>>;

public class GetOwnersForBuildingSelectQueryHandler(UserManager<ApplicationUser> userManager)
    : IRequestHandler<GetOwnersForBuildingSelectQuery, IEnumerable<SelectListItem>>
{
    public async Task<IEnumerable<SelectListItem>> Handle(GetOwnersForBuildingSelectQuery request, CancellationToken cancellationToken = default)
    {
        var ownerUsers = await userManager.GetUsersInRoleAsync(Roles.Owner);
        return ownerUsers
            .Where(u => u.BuildingId == request.BuildingId)
            .OrderBy(u => u.Fullname)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = string.IsNullOrWhiteSpace(u.Fullname) ? u.Email! : $"{u.Fullname} ({u.Email})"
            })
            .ToList();
    }
}


