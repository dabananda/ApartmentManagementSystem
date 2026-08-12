using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Domain.Entities;
using AMS.Application.Features.Tenancy.DTOs;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantPaymentHistoryQuery(string TenantUserId) : IRequest<List<TenantPaymentRecord>>;

public class GetTenantPaymentHistoryQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantPaymentHistoryQuery, List<TenantPaymentRecord>>
{
    public Task<List<TenantPaymentRecord>> Handle(GetTenantPaymentHistoryQuery request, CancellationToken cancellationToken = default)
        => repository.GetTenantPaymentHistoryAsync(request.TenantUserId, cancellationToken);
}
