using AMS.Application.Interfaces.Payments;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Payments.Commands;

public record CreateOwnerCheckoutSessionCommand(Guid CommonBillId, string OwnerId, string SuccessUrl, string CancelUrl) : IRequest<(bool success, string message, string? url)>;

public class CreateOwnerCheckoutSessionCommandHandler(IStripePaymentService stripeService)
    : IRequestHandler<CreateOwnerCheckoutSessionCommand, (bool success, string message, string? url)>
{
    public Task<(bool success, string message, string? url)> Handle(CreateOwnerCheckoutSessionCommand request, CancellationToken cancellationToken = default)
    {
        return stripeService.CreateOwnerCheckoutSessionAsync(request.CommonBillId, request.OwnerId, request.SuccessUrl, request.CancelUrl);
    }
}
