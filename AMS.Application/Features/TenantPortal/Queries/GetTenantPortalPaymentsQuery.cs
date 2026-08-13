using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalPaymentsQuery(string TenantUserId) : IRequest<IEnumerable<TenantPaymentRow>>;

public class GetTenantPortalPaymentsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalPaymentsQuery, IEnumerable<TenantPaymentRow>>
{
    public Task<IEnumerable<TenantPaymentRow>> Handle(GetTenantPortalPaymentsQuery request, CancellationToken cancellationToken = default)
        => repository.GetPaymentsAsync(request.TenantUserId, cancellationToken);
}


