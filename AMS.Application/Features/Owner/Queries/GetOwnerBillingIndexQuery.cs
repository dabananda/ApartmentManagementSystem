using AMS.Application.Features.Owner.DTOs;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnerBillingIndexQuery(Guid BuildingId) : IRequest<IReadOnlyList<OwnerBillingRow>>;

public class GetOwnerBillingIndexQueryHandler(IOwnerBillingRepository repository)
    : IRequestHandler<GetOwnerBillingIndexQuery, IReadOnlyList<OwnerBillingRow>>
{
    public Task<IReadOnlyList<OwnerBillingRow>> Handle(GetOwnerBillingIndexQuery request, CancellationToken cancellationToken = default)
        => repository.GetIndexRowsAsync(request.BuildingId, cancellationToken);
}
