using AMS.Application.Interfaces.Administration;
using AMS.Application.Mediator;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.Queries;

public record GetBuildingSelectItemsQuery(Guid? RestrictToBuildingId = null) : IRequest<List<SelectListItem>>;

public class GetBuildingSelectItemsQueryHandler(IUserManagementRepository repository)
    : IRequestHandler<GetBuildingSelectItemsQuery, List<SelectListItem>>
{
    public Task<List<SelectListItem>> Handle(GetBuildingSelectItemsQuery request, CancellationToken cancellationToken = default)
    {
        return repository.GetBuildingSelectItemsAsync(request.RestrictToBuildingId, cancellationToken);
    }
}
