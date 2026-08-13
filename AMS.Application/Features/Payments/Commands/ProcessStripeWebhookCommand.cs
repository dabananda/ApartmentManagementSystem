using AMS.Application.Interfaces.Payments;
using AMS.Application.Mediator;

namespace AMS.Application.Features.Payments.Commands;

public record ProcessStripeWebhookCommand(string Json, string Signature) : IRequest;

public class ProcessStripeWebhookCommandHandler(IStripePaymentService stripeService)
    : IRequestHandler<ProcessStripeWebhookCommand>
{
    public async Task Handle(ProcessStripeWebhookCommand request, CancellationToken cancellationToken = default)
    {
        await stripeService.ProcessWebhookEventAsync(request.Json, request.Signature);
    }
}
