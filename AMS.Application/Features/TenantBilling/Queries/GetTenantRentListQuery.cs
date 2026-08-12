using AMS.Application.Features.Tenancy.DTOs;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantRentListQuery(string? RestrictToOwnerId) : IRequest<List<TenantRentListRow>>;

public class GetTenantRentListQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantRentListQuery, List<TenantRentListRow>>
{
    public Task<List<TenantRentListRow>> Handle(GetTenantRentListQuery request, CancellationToken cancellationToken = default)
        => repository.GetTenantRentListAsync(request.RestrictToOwnerId, cancellationToken);
}
