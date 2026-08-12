using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Application.Interfaces.TenantPortal;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantPortal.Queries;

public record GetTenantPortalPaymentsQuery(string TenantUserId) : IRequest<List<TenantPaymentRow>>;

public class GetTenantPortalPaymentsQueryHandler(ITenantPortalRepository repository)
    : IRequestHandler<GetTenantPortalPaymentsQuery, List<TenantPaymentRow>>
{
    public Task<List<TenantPaymentRow>> Handle(GetTenantPortalPaymentsQuery request, CancellationToken cancellationToken = default)
        => repository.GetPaymentsAsync(request.TenantUserId, cancellationToken);
}
