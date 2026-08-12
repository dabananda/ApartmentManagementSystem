using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalBillsQuery(string TenantUserId) : IRequest<List<TenantBillRow>>;

public class GetTenantPortalBillsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalBillsQuery, List<TenantBillRow>>
{
    public Task<List<TenantBillRow>> Handle(GetTenantPortalBillsQuery request, CancellationToken cancellationToken = default)
        => repository.GetBillsAsync(request.TenantUserId, cancellationToken);
}
