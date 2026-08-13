using AMS.Application.Features.Payments.DTOs;
using AMS.Application.Interfaces.Payments;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Payments.Queries;

public record GetCheckoutResultQuery(string SessionId) : IRequest<CheckoutResultVm?>;

public class GetCheckoutResultQueryHandler(IStripePaymentService stripeService)
    : IRequestHandler<GetCheckoutResultQuery, CheckoutResultVm?>
{
    public Task<CheckoutResultVm?> Handle(GetCheckoutResultQuery request, CancellationToken cancellationToken = default)
    {
        return stripeService.GetCheckoutResultAsync(request.SessionId);
    }
}
