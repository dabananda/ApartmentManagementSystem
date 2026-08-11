namespace ApartmentManagementSystem.Features.Payments
{
    public class StripeOptions
    {
        public string? PublishableKey { get; set; }
        public string? SecretKey { get; set; }
        public string? WebhookSecret { get; set; }
        public string Currency { get; set; } = "bdt";
    }
}
