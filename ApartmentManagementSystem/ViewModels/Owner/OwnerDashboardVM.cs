namespace ApartmentManagementSystem.ViewModels.Owner
{
    public class OwnerDashboardVM
    {
        // Flats
        public int FlatsOwnedCount { get; set; }
        public int FlatsOccupiedCount { get; set; }

        // Tenant Rent (from TenantBills/TenantPayments)
        public decimal RentTotalBilled { get; set; }
        public decimal RentTotalPaid { get; set; }
        public decimal RentTotalDue => RentTotalBilled - RentTotalPaid;
        public decimal RentPaidThisMonth { get; set; }

        // Common Bills (from ExpenseAllocations/ExpenseAllocationPayments)
        public decimal CommonTotalBilled { get; set; }  // sum of all allocations for this owner
        public decimal CommonTotalPaid { get; set; }    // sum of payments by this owner
        public decimal CommonTotalDue => CommonTotalBilled - CommonTotalPaid;

        // Tenants (active)
        public List<OwnerTenantRow> Tenants { get; set; } = new();

        // Recent payments
        public List<OwnerRecentRentPaymentRow> RecentRent { get; set; } = new();
        public List<OwnerRecentCommonPaymentRow> RecentCommon { get; set; } = new();
    }

    public class OwnerTenantRow
    {
        public string TenantUserId { get; set; } = default!;
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string FlatNumber { get; set; } = "";
        public DateTime From { get; set; }
    }

    public class OwnerRecentRentPaymentRow
    {
        public Guid PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string TenantName { get; set; } = "";
        public string FlatNumber { get; set; } = "";
        public string? Reference { get; set; }
    }

    public class OwnerRecentCommonPaymentRow
    {
        public Guid PaymentId { get; set; }
        public DateTime PaymentDate { get; set; }
        public decimal Amount { get; set; }
        public string BillTitle { get; set; } = "";
        public DateTime BillDate { get; set; }
        public string? Reference { get; set; }
    }

    public class OwnerDashboardRentVM
    {
        public decimal TotalBilled { get; set; }
        public decimal TotalPaid { get; set; }
        public decimal TotalDue => TotalBilled - TotalPaid;

        public decimal MonthPaid { get; set; }
        public int OpenBillCount { get; set; }

        public List<OwnerRecentRentPaymentRow> Recent { get; set; } = new();
    }
}
