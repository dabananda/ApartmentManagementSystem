using ApartmentManagementSystem.Features.Payments.ViewModels;

namespace ApartmentManagementSystem.Features.Payments.Services;

public interface IStripePaymentService
{
    Task<(bool success, string message, string? checkoutUrl)> CreateTenantCheckoutSessionAsync(Guid billId, string tenantUserId, decimal? amountRequested, string successUrlTemplate, string cancelUrl);
    Task<(bool success, string message, string? checkoutUrl)> CreateOwnerCheckoutSessionAsync(Guid commonBillId, string ownerId, string successUrlTemplate, string cancelUrl);
    
    Task<CheckoutResultVm?> GetCheckoutResultAsync(string sessionId);
    
    Task ProcessWebhookEventAsync(string json, string signature);
}
