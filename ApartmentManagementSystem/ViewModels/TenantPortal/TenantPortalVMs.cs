using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.ViewModels.TenantPortal
{
    public class TenantDashboardVM
    {
        public string TenantName { get; set; } = "";
        public string BuildingName { get; set; } = "";
        public string FlatNumber { get; set; } = "";

        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue => TotalBilled - TotalPaid;
        public decimal PaidThisMonth { get; set; }

        public List<TenantBillRow> RecentBills { get; set; } = new();
        public List<TenantPaymentRow> RecentPayments { get; set; } = new();
        public List<Announcement> RecentNotices { get; set; } = new();
    }

    public class TenantBillRow
    {
        public Guid BillId { get; set; }
        public string Title { get; set; } = "";
        public DateTime BillDate { get; set; }
        public decimal Amount { get; set; }
        public decimal Paid { get; set; }
        public decimal Due => Amount - Paid;

        public string? BuildingName { get; set; }
        public string? FlatNumber { get; set; }
    }

    public class TenantPaymentRow
    {
        public Guid PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string? Reference { get; set; }
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }

        public string? BuildingName { get; set; }
        public string? FlatNumber { get; set; }
    }
}
