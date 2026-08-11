using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Administration.ViewModels
{
    public class SuperAdminDashboardViewModel
    {
        public int TotalBuildings { get; set; }
        public List<BuildingSummaryViewModel> BuildingsSummary { get; set; } = new List<BuildingSummaryViewModel>();

        public int TotalUsers { get; set; }
        public int TotalSuperAdmins { get; set; }
        public int TotalPresidents { get; set; }
        public int TotalOwners { get; set; }
        public int TotalStaffs { get; set; }
        public int PendingApprovals { get; set; }
        public int TotalTenants { get; set; }

        public int TotalFlats { get; set; }
        public int OccupiedFlats { get; set; }
        public int VacantFlats { get; set; }
        public int FlatsWithOwners { get; set; }
        public int FlatsWithoutOwners { get; set; }

        public decimal TotalBillsGenerated { get; set; }
        public decimal TotalPaymentsMade { get; set; }
        public decimal TotalAmountCollected { get; set; }
        public decimal TotalPendingCollection { get; set; }
        public decimal OverallBalance { get; set; }

        public List<CommonBill> RecentBills { get; set; } = new List<CommonBill>();
        public List<ExpensePayment> RecentPayments { get; set; } = new List<ExpensePayment>();
    }

    public class BuildingSummaryViewModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int TotalFlats { get; set; }
        public int OccupiedFlats { get; set; }
        public int VacantFlats => TotalFlats - OccupiedFlats;
        public decimal TotalBills { get; set; }
        public decimal TotalPayments { get; set; }
        public decimal Balance { get; set; }
    }
}
