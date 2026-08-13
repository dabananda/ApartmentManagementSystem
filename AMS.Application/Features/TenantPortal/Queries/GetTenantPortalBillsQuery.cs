using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalBillsQuery(string TenantUserId) : IRequest<IEnumerable<TenantBillRow>>;

public class GetTenantPortalBillsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalBillsQuery, IEnumerable<TenantBillRow>>
{
    public Task<IEnumerable<TenantBillRow>> Handle(GetTenantPortalBillsQuery request, CancellationToken cancellationToken = default)
        => repository.GetBillsAsync(request.TenantUserId, cancellationToken);
}


