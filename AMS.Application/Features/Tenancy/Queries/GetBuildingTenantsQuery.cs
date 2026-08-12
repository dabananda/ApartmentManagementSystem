using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Features.Buildings.DTOs;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetBuildingTenantsQuery(Guid BuildingId) : IRequest<IReadOnlyList<BuildingTenantRow>>;

public class GetBuildingTenantsQueryHandler(ITenantDirectoryRepository repo)
    : IRequestHandler<GetBuildingTenantsQuery, IReadOnlyList<BuildingTenantRow>>
{
    public Task<IReadOnlyList<BuildingTenantRow>> Handle(GetBuildingTenantsQuery request, CancellationToken cancellationToken = default)
        => repo.GetBuildingTenantsAsync(request.BuildingId, cancellationToken);
}
