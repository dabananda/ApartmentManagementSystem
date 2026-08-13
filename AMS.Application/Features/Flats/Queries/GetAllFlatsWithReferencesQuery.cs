using AMS.Application.Interfaces.Flats;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.Queries;

public record GetAllFlatsWithReferencesQuery : IRequest<IReadOnlyList<Flat>>;

public class GetAllFlatsWithReferencesQueryHandler(IFlatRepository flats)
    : IRequestHandler<GetAllFlatsWithReferencesQuery, IReadOnlyList<Flat>>
{
    public Task<IReadOnlyList<Flat>> Handle(GetAllFlatsWithReferencesQuery request, CancellationToken cancellationToken) =>
        flats.GetAllWithReferencesAsync(cancellationToken);
}
