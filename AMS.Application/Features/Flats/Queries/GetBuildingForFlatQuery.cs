using AMS.Application.Interfaces.Buildings;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Queries;

public record GetBuildingForFlatQuery(Guid BuildingId) : IRequest<Building?>;

public class GetBuildingForFlatQueryHandler(IBuildingRepository buildings)
    : IRequestHandler<GetBuildingForFlatQuery, Building?>
{
    public Task<Building?> Handle(GetBuildingForFlatQuery request, CancellationToken cancellationToken) =>
        buildings.GetAsync(request.BuildingId, false, cancellationToken);
}
