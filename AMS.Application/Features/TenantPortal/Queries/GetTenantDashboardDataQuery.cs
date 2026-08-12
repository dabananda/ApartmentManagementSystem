using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantDashboardDataQuery(string TenantUserId) : IRequest<TenantDashboardVM?>;

public class GetTenantDashboardDataQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantDashboardDataQuery, TenantDashboardVM?>
{
    public Task<TenantDashboardVM?> Handle(GetTenantDashboardDataQuery request, CancellationToken cancellationToken = default)
        => repository.GetDashboardDataAsync(request.TenantUserId, cancellationToken);
}
