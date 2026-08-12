using System.Text.Json.Serialization;

namespace AMS.Application.Features.President.DTOs
{
    public class PresidentDashboardViewModel
    {
        public string BuildingName { get; set; } = "(Building)";
        public int TotalBills { get; set; }
        public decimal TotalCollected { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal CollectedThisMonth { get; set; }
        public decimal PaymentsThisMonth { get; set; }
        public int TotalFlats { get; set; }
        public int OccupiedFlats { get; set; }
        public int TodayEntries { get; set; }
        public int Last7dEntries { get; set; }

        public List<TransactionRowViewModel> RecentTransactions { get; set; } = new();

        public CashflowChartVM Cashflow { get; set; } = new();
        public AgingBucketsVM Aging { get; set; } = new();
        public TopOwnersVM TopOwners { get; set; } = new();
    }

    public class TransactionRowViewModel
    {
        public DateTime OccurredAt { get; set; }
        public string Type { get; set; } = "";
        public string Description { get; set; } = "";
        public decimal? Amount { get; set; }
        public string? Currency { get; set; }
        public string Direction { get; set; } = "Info";
    }

    public class CashflowChartVM
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> In { get; set; } = new();
        public List<decimal> Out { get; set; } = new();
    }

    public class AgingBucketsVM
    {
        public decimal D0_30 { get; set; }
        public decimal D31_60 { get; set; }
        public decimal D61_90 { get; set; }
        public decimal D90Plus { get; set; }

        [JsonIgnore]
        public bool HasAny => (D0_30 + D31_60 + D61_90 + D90Plus) > 0m;
    }

    public class TopOwnersVM
    {
        public List<string> Labels { get; set; } = new();
        public List<decimal> Values { get; set; } = new();
    }
}
