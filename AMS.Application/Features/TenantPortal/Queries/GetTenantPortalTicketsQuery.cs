using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalTicketsQuery(Guid BuildingId, Guid FlatId, string TenantUserId) : IRequest<List<MaintenanceTicket>>;

public class GetTenantPortalTicketsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalTicketsQuery, List<MaintenanceTicket>>
{
    public Task<List<MaintenanceTicket>> Handle(GetTenantPortalTicketsQuery request, CancellationToken cancellationToken = default)
        => repository.GetTicketsAsync(request.BuildingId, request.FlatId, request.TenantUserId, cancellationToken);
}
