using AMS.Application.Features.Payments.DTOs;
using AMS.Application.Interfaces.Payments;
using AMS.Application.Configuration;
using Stripe;
using Microsoft.Extensions.Logging;
using Stripe.Checkout;

namespace AMS.Infrastructure.Services;

public sealed class StripePaymentService(
    IPaymentRepository repository,
    IPaymentEmailService paymentEmailService,
    StripeClient stripeClient,
    AppSettings appSettings,
    ILogger<StripePaymentService> log) : IStripePaymentService
{
    private readonly StripeSettings _opts = appSettings.Stripe;

    public async Task<(bool success, string message, string? checkoutUrl)> CreateTenantCheckoutSessionAsync(Guid billId, string tenantUserId, decimal? amountRequested, string successUrlTemplate, string cancelUrl)
    {
        var bill = await repository.GetTenantBillForCheckoutAsync(billId, tenantUserId);
        if (bill == null) return (false, "Bill not found.", null);

        var paidNow = await repository.GetPaidAmountForTenantBillAsync(billId);
        var dueNow = bill.Amount - paidNow;
        if (dueNow <= 0m) return (false, "No due on this bill.", null);

        var take = Math.Min(amountRequested ?? dueNow, dueNow);
        if (take <= 0m) return (false, "Nothing to pay.", null);

        var cents = (long)Math.Round(take * 100m, MidpointRounding.AwayFromZero);
        var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "bdt" : _opts.Currency.ToLowerInvariant();
        var sessionService = new SessionService(stripeClient);

        var meta = new Dictionary<string, string>
        {
            ["kind"] = "tenant",
            ["tenantBillId"] = bill.Id.ToString(),
            ["tenantUserId"] = tenantUserId,
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrlTemplate,
            CancelUrl = cancelUrl,
            CustomerEmail = bill.TenantUser?.Email,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = cents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Rent — {bill.Title} ({bill.BillDate:yyyy-MM})",
                            Description = $"{bill.Flat!.Building!.Name} / Flat {bill.Flat!.FlatNumber}"
                        }
                    }
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Description = $"Rent {bill.BillDate:yyyy-MM} — {bill.Title}",
                Metadata = meta
            },
            Metadata = meta
        };

        var session = await sessionService.CreateAsync(options);
        return (true, string.Empty, session.Url);
    }

    public async Task<(bool success, string message, string? checkoutUrl)> CreateOwnerCheckoutSessionAsync(Guid commonBillId, string ownerId, string successUrlTemplate, string cancelUrl)
    {
        var alloc = await repository.GetExpenseAllocationForCheckoutAsync(commonBillId, ownerId);
        if (alloc == null) return (false, "Allocation not found.", null);

        var paid = await repository.GetPaidAmountForAllocationAsync(commonBillId, ownerId);
        var due = alloc.AmountDue - paid;
        if (due <= 0m) return (false, "No due on this bill.", null);

        var cents = (long)Math.Round(due * 100m, MidpointRounding.AwayFromZero);
        var currency = string.IsNullOrWhiteSpace(_opts.Currency) ? "bdt" : _opts.Currency.ToLowerInvariant();
        var sessionService = new SessionService(stripeClient);

        var meta = new Dictionary<string, string>
        {
            ["kind"] = "owner",
            ["ownerId"] = ownerId,
            ["commonBillId"] = alloc.CommonBillId.ToString()
        };

        var options = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = successUrlTemplate,
            CancelUrl = cancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new()
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = currency,
                        UnitAmount = cents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = $"Common Bill — {alloc.CommonBill!.Name}",
                            Description = $"{alloc.CommonBill!.Building!.Name} — Owner payment"
                        }
                    }
                }
            },
            PaymentIntentData = new SessionPaymentIntentDataOptions
            {
                Description = $"Common Bill — {alloc.CommonBill!.Name}",
                Metadata = meta
            },
            Metadata = meta
        };

        var session = await sessionService.CreateAsync(options);
        return (true, string.Empty, session.Url);
    }

    public async Task<CheckoutResultVm?> GetCheckoutResultAsync(string sessionId)
    {
        if (string.IsNullOrWhiteSpace(sessionId)) return null;

        var sessionSvc = new SessionService(stripeClient);
        var session = await sessionSvc.GetAsync(sessionId);

        string? piId = session.PaymentIntentId ?? session.PaymentIntent?.Id;
        string? status = null;

        if (!string.IsNullOrWhiteSpace(piId))
        {
            var piSvc = new PaymentIntentService(stripeClient);
            var pi = await piSvc.GetAsync(piId);
            status = pi.Status;
        }

        return new CheckoutResultVm
        {
            SessionId = session.Id,
            PaymentIntentId = piId,
            Status = status,
            Amount = (session.AmountTotal ?? 0) / 100m,
            Currency = (session.Currency ?? "usd").ToUpperInvariant()
        };
    }

    public async Task ProcessWebhookEventAsync(string json, string signature)
    {
        var endpointSecret = appSettings.Stripe.WebhookSecret;
        var stripeEvent = EventUtility.ConstructEvent(json, signature, endpointSecret);
        log.LogInformation("Stripe webhook received: {Type} ({Id})", stripeEvent.Type, stripeEvent.Id);

        var t = stripeEvent.Type?.ToLowerInvariant();

        if (t == "payment_intent.succeeded")
        {
            var intent = (PaymentIntent)stripeEvent.Data.Object;
            await HandlePaymentIntentSucceeded(intent);
        }
        else if (t == "checkout.session.completed")
        {
            var session = (Session)stripeEvent.Data.Object;
            await HandleCheckoutSessionCompleted(session);
        }
        else
        {
            log.LogInformation("Unhandled Stripe event type: {Type}", stripeEvent.Type);
        }
    }

    private async Task HandleCheckoutSessionCompleted(Session session)
    {
        if (!string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase))
        {
            log.LogInformation("Checkout session not paid. Status: {Status}", session.PaymentStatus);
            return;
        }

        string? piId = session.PaymentIntentId;
        if (string.IsNullOrWhiteSpace(piId) && session.PaymentIntent != null)
            piId = session.PaymentIntent.Id;

        if (string.IsNullOrWhiteSpace(piId))
        {
            log.LogWarning("checkout.session.completed without PaymentIntent id.");
            return;
        }

        var amount = (session.AmountTotal ?? 0) / 100m;

        IDictionary<string, string> meta = session.Metadata ?? new Dictionary<string, string>();
        if (!meta.ContainsKey("kind"))
        {
            var piSvc = new PaymentIntentService(stripeClient);
            var intent = await piSvc.GetAsync(piId);
            meta = intent.Metadata ?? meta;
            if (meta.ContainsKey("kind"))
            {
                long cents = intent.AmountReceived;
                if (cents <= 0) cents = intent.Amount;
                if (amount <= 0) amount = cents / 100m;
            }
        }

        await ProcessPaymentFromMetaAsync(meta, amount, piId);
    }

    private async Task HandlePaymentIntentSucceeded(PaymentIntent intent)
    {
        long cents = intent.AmountReceived;
        if (cents <= 0) cents = intent.Amount;
        var amountReceived = cents / 100m;

        var meta = intent.Metadata ?? new Dictionary<string, string>();
        await ProcessPaymentFromMetaAsync(meta, amountReceived, intent.Id);
    }

    private async Task ProcessPaymentFromMetaAsync(IDictionary<string, string> meta, decimal amountReceived, string paymentRef)
    {
        if (!meta.TryGetValue("kind", out var kind) || string.IsNullOrWhiteSpace(kind))
            return;

        if (string.Equals(kind, "tenant", StringComparison.OrdinalIgnoreCase) &&
            meta.TryGetValue("tenantBillId", out var billIdStr) &&
            Guid.TryParse(billIdStr, out var billId) &&
            meta.TryGetValue("tenantUserId", out var tenantUserId))
        {
            var (success, payment) = await repository.ProcessTenantPaymentFromWebhookAsync(billId, tenantUserId, amountReceived, paymentRef);
            if (success && payment != null)
            {
                try
                {
                    await paymentEmailService.SendTenantPaymentEmailAsync(tenantUserId, [payment]);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error sending webhook tenant payment receipt email for {PaymentId}", payment.Id);
                }
            }
        }
        else if (string.Equals(kind, "owner", StringComparison.OrdinalIgnoreCase) &&
                 meta.TryGetValue("ownerId", out var ownerId) &&
                 meta.TryGetValue("commonBillId", out var cbStr) &&
                 Guid.TryParse(cbStr, out var commonBillId))
        {
            var (success, payment) = await repository.ProcessOwnerPaymentFromWebhookAsync(commonBillId, ownerId, amountReceived, paymentRef);
            if (success && payment != null)
            {
                try
                {
                    await paymentEmailService.SendOwnerPaymentEmailAsync(ownerId, [payment], null);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, "Error sending webhook owner payment receipt email for {PaymentId}", payment.Id);
                }
            }
        }
    }
}
