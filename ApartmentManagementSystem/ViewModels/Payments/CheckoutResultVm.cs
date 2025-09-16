namespace ApartmentManagementSystem.ViewModels.Payments
{
    public class CheckoutResultVm
    {
        public string SessionId { get; set; } = "";
        public string? PaymentIntentId { get; set; }
        public string? Status { get; set; }
        public decimal Amount { get; set; }
        public string Currency { get; set; } = "USD";
    }
}
