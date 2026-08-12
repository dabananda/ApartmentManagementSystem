using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Application.Features.Reports.DTOs
{
    public class ReportDashboardViewModel
    {
        public string BuildingName { get; set; } = default!;

        [Display(Name = "Total Bills Issued")]
        [DataType(DataType.Currency)]
        public decimal TotalBills { get; set; }

        [Display(Name = "Total Collected from Owners")]
        [DataType(DataType.Currency)]
        public decimal TotalCollected { get; set; }

        [Display(Name = "Total Bills Paid")]
        [DataType(DataType.Currency)]
        public decimal TotalPayments { get; set; }

        [Display(Name = "Current Balance")]
        [DataType(DataType.Currency)]
        public decimal Balance { get; set; }

        public IEnumerable<ExpenseAllocation>? Allocations { get; set; }
    }
}
