using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalNoticesQuery(Guid? BuildingId) : IRequest<IEnumerable<Announcement>>;

public class GetTenantPortalNoticesQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalNoticesQuery, IEnumerable<Announcement>>
{
    public Task<IEnumerable<Announcement>> Handle(GetTenantPortalNoticesQuery request, CancellationToken cancellationToken = default)
        => repository.GetNoticesAsync(request.BuildingId, cancellationToken);
}


