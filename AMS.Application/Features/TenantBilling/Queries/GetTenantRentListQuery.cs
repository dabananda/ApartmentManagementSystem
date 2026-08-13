using AMS.Application.Features.Tenancy.DTOs;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantRentListQuery(string? RestrictToOwnerId) : IRequest<IEnumerable<TenantRentListRow>>;

public class GetTenantRentListQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantRentListQuery, IEnumerable<TenantRentListRow>>
{
    public Task<IEnumerable<TenantRentListRow>> Handle(GetTenantRentListQuery request, CancellationToken cancellationToken = default)
        => repository.GetTenantRentListAsync(request.RestrictToOwnerId, cancellationToken);
}


