using AMS.Application.Interfaces.Administration;
using AMS.Application.Mediator;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.Queries;

public record GetBuildingSelectItemsQuery(Guid? RestrictToBuildingId = null) : IRequest<IEnumerable<SelectListItem>>;

public class GetBuildingSelectItemsQueryHandler(IUserManagementRepository repository)
    : IRequestHandler<GetBuildingSelectItemsQuery, IEnumerable<SelectListItem>>
{
    public Task<IEnumerable<SelectListItem>> Handle(GetBuildingSelectItemsQuery request, CancellationToken cancellationToken = default)
    {
        return repository.GetBuildingSelectItemsAsync(request.RestrictToBuildingId, cancellationToken);
    }
}


