using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalVisitorsQuery(Guid BuildingId, Guid FlatId, DateTime? From, DateTime? To) : IRequest<IEnumerable<EntryLog>>;

public class GetTenantPortalVisitorsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalVisitorsQuery, IEnumerable<EntryLog>>
{
    public Task<IEnumerable<EntryLog>> Handle(GetTenantPortalVisitorsQuery request, CancellationToken cancellationToken = default)
        => repository.GetVisitorsAsync(request.BuildingId, request.FlatId, request.From, request.To, cancellationToken);
}


