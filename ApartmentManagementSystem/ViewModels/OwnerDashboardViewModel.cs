using ApartmentManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class OwnerDashboardViewModel
    {
        public string? OwnerName { get; set; }
        public string? BuildingName { get; set; }
        public string? BuildingAddress { get; set; }

        public int TotalFlatsOwned { get; set; }
        public int OccupiedFlats { get; set; }
        public int VacantFlats { get; set; }

        [Display(Name = "Total Bills Due")]
        public decimal TotalBillsDue { get; set; }

        [Display(Name = "Total Bills Paid")]
        public decimal TotalBillsPaid { get; set; }

        [Display(Name = "Total Rent Collected")]
        public decimal TotalRentCollected { get; set; }

        [Display(Name = "Financial Balance")]
        public decimal FinancialBalance { get; set; }

        public IEnumerable<ExpenseAllocation>? ExpenseAllocations { get; set; }
        public IEnumerable<Rent>? RentCollections { get; set; }
    }
}
