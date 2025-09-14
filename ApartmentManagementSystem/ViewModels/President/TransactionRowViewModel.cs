namespace ApartmentManagementSystem.ViewModels.President
{
    public class TransactionRowViewModel
    {
        public DateTime OccurredAt { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string Direction { get; set; } = "";
    }
}
