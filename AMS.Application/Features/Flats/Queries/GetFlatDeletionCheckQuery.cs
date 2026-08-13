using AMS.Application.Features.Flats.DTOs;
using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Flats.Queries;

public record GetFlatDeletionCheckQuery(Guid FlatId) : IRequest<FlatDeletionCheck>;

public class GetFlatDeletionCheckQueryHandler(IFlatRepository flats)
    : IRequestHandler<GetFlatDeletionCheckQuery, FlatDeletionCheck>
{
    public Task<FlatDeletionCheck> Handle(GetFlatDeletionCheckQuery request, CancellationToken cancellationToken) =>
        flats.GetDeletionCheckAsync(request.FlatId, cancellationToken);
}
