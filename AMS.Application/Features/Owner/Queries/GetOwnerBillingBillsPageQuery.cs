using AMS.Application.Mediator;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Features.Owner.DTOs;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnerBillingBillsPageQuery(string OwnerId, Guid? RestrictToBuildingId) : IRequest<OwnerBillsPage?>;

public class GetOwnerBillingBillsPageQueryHandler(IOwnerBillingRepository repository)
    : IRequestHandler<GetOwnerBillingBillsPageQuery, OwnerBillsPage?>
{
    public Task<OwnerBillsPage?> Handle(GetOwnerBillingBillsPageQuery request, CancellationToken cancellationToken = default)
        => repository.GetBillsPageAsync(request.OwnerId, request.RestrictToBuildingId, cancellationToken);
}
