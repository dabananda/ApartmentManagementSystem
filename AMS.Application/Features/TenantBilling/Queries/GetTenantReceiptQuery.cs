using AMS.Application.Mediator;
using AMS.Application.Interfaces.TenantBilling;
using AMS.Domain.Entities;

namespace AMS.Application.Features.TenantBilling.Queries;

public record GetTenantReceiptQuery(Guid PaymentId) : IRequest<(TenantPayment? payment, string ownerId)>;

public class GetTenantReceiptQueryHandler(ITenantRentRepository repository)
    : IRequestHandler<GetTenantReceiptQuery, (TenantPayment? payment, string ownerId)>
{
    public Task<(TenantPayment? payment, string ownerId)> Handle(GetTenantReceiptQuery request, CancellationToken cancellationToken = default)
        => repository.GetReceiptDataAsync(request.PaymentId, cancellationToken);
}
