using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantBillsPageQuery(string TenantUserId) : IRequest<TenantBillsPage?>;

public class GetTenantBillsPageQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantBillsPageQuery, TenantBillsPage?>
{
    public Task<TenantBillsPage?> Handle(GetTenantBillsPageQuery request, CancellationToken cancellationToken = default)
        => repository.GetTenantBillsPageAsync(request.TenantUserId, cancellationToken);
}
