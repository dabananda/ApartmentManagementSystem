using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Queries;

public record GetFlatsForBuildingQuery(Guid BuildingId) : IRequest<IReadOnlyList<Flat>>;

public class GetFlatsForBuildingQueryHandler(IFlatRepository flats)
    : IRequestHandler<GetFlatsForBuildingQuery, IReadOnlyList<Flat>>
{
    public Task<IReadOnlyList<Flat>> Handle(GetFlatsForBuildingQuery request, CancellationToken cancellationToken) =>
        flats.GetForBuildingAsync(request.BuildingId, cancellationToken);
}
