using AMS.Application.Mediator;
using AMS.Application.Interfaces.Tenancy;
using AMS.Application.Features.Flats.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.Queries;

public record GetFlatTenantsQuery(Flat Flat) : IRequest<IReadOnlyList<FlatTenantRow>>;

public class GetFlatTenantsQueryHandler(ITenantDirectoryRepository repo)
    : IRequestHandler<GetFlatTenantsQuery, IReadOnlyList<FlatTenantRow>>
{
    public Task<IReadOnlyList<FlatTenantRow>> Handle(GetFlatTenantsQuery request, CancellationToken cancellationToken = default)
        => repo.GetFlatTenantsAsync(request.Flat, cancellationToken);
}
