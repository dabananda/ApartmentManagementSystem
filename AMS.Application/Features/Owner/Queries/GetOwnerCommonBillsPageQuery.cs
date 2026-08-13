using AMS.Application.Features.Owner.DTOs;
using AMS.Application.Interfaces.Owner;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Owner.Queries;

public record GetOwnerCommonBillsPageQuery(string OwnerId, Guid? RestrictToBuildingId) : IRequest<OwnerBillsPage?>;

public class GetOwnerCommonBillsPageQueryHandler(IOwnerRepository repository)
    : IRequestHandler<GetOwnerCommonBillsPageQuery, OwnerBillsPage?>
{
    public Task<OwnerBillsPage?> Handle(GetOwnerCommonBillsPageQuery request, CancellationToken cancellationToken = default)
        => repository.GetCommonBillsPageAsync(request.OwnerId, request.RestrictToBuildingId, cancellationToken);
}
