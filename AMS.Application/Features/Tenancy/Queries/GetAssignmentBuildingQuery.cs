using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetAssignmentBuildingQuery(Guid BuildingId) : IRequest<Building?>;

public class GetAssignmentBuildingQueryHandler(ITenantDirectoryRepository repo)
    : IRequestHandler<GetAssignmentBuildingQuery, Building?>
{
    public Task<Building?> Handle(GetAssignmentBuildingQuery request, CancellationToken cancellationToken = default)
        => repo.GetBuildingAsync(request.BuildingId, cancellationToken);
}
