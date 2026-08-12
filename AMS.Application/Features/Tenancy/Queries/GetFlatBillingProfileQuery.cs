using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetFlatBillingProfileQuery(Guid FlatId) : IRequest<FlatBillingProfile?>;

public class GetFlatBillingProfileQueryHandler(IFlatBillingProfileRepository repo)
    : IRequestHandler<GetFlatBillingProfileQuery, FlatBillingProfile?>
{
    public Task<FlatBillingProfile?> Handle(GetFlatBillingProfileQuery request, CancellationToken cancellationToken = default)
        => repo.GetProfileAsync(request.FlatId, cancellationToken);
}
