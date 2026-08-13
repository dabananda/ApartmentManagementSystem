using AMS.Application.Features.Tenancy.DTOs;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Application.Mediator;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantPaymentHistoryQuery(string TenantUserId) : IRequest<IEnumerable<TenantPaymentRecord>>;

public class GetTenantPaymentHistoryQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantPaymentHistoryQuery, IEnumerable<TenantPaymentRecord>>
{
    public Task<IEnumerable<TenantPaymentRecord>> Handle(GetTenantPaymentHistoryQuery request, CancellationToken cancellationToken = default)
        => repository.GetTenantPaymentHistoryAsync(request.TenantUserId, cancellationToken);
}


