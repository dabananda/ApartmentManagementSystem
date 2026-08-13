using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Queries;

public record GetFlatByIdQuery(Guid FlatId, bool IncludeReferences = false, bool AsNoTracking = false) : IRequest<Flat?>;

public class GetFlatByIdQueryHandler(IFlatRepository flats) : IRequestHandler<GetFlatByIdQuery, Flat?>
{
    public Task<Flat?> Handle(GetFlatByIdQuery request, CancellationToken cancellationToken) =>
        flats.GetAsync(request.FlatId, request.IncludeReferences, request.AsNoTracking, cancellationToken);
}
