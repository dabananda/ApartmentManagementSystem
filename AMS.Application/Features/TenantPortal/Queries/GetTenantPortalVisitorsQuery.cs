using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalVisitorsQuery(Guid BuildingId, Guid FlatId, DateTime? From, DateTime? To) : IRequest<List<EntryLog>>;

public class GetTenantPortalVisitorsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalVisitorsQuery, List<EntryLog>>
{
    public Task<List<EntryLog>> Handle(GetTenantPortalVisitorsQuery request, CancellationToken cancellationToken = default)
        => repository.GetVisitorsAsync(request.BuildingId, request.FlatId, request.From, request.To, cancellationToken);
}
