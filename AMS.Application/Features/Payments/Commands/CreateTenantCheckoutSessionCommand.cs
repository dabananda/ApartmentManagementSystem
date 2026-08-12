using AMS.Application.Interfaces.Payments;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Payments.Commands;

public record CreateTenantCheckoutSessionCommand(Guid BillId, string TenantId, decimal? Amount, string SuccessUrl, string CancelUrl) : IRequest<(bool success, string message, string? url)>;

public class CreateTenantCheckoutSessionCommandHandler(IStripePaymentService stripeService)
    : IRequestHandler<CreateTenantCheckoutSessionCommand, (bool success, string message, string? url)>
{
    public Task<(bool success, string message, string? url)> Handle(CreateTenantCheckoutSessionCommand request, CancellationToken cancellationToken = default)
    {
        return stripeService.CreateTenantCheckoutSessionAsync(request.BillId, request.TenantId, request.Amount, request.SuccessUrl, request.CancelUrl);
    }
}
