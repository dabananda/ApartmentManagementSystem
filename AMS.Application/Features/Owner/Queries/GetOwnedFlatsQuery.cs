using AMS.Application.Mediator;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Features.Owner.DTOs;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnedFlatsQuery(string OwnerId) : IRequest<IReadOnlyList<OwnerOwnedFlatRow>>;

public class GetOwnedFlatsQueryHandler(IOwnerRepository repository)
    : IRequestHandler<GetOwnedFlatsQuery, IReadOnlyList<OwnerOwnedFlatRow>>
{
    public Task<IReadOnlyList<OwnerOwnedFlatRow>> Handle(GetOwnedFlatsQuery request, CancellationToken cancellationToken = default)
        => repository.GetOwnedFlatsAsync(request.OwnerId, cancellationToken);
}
