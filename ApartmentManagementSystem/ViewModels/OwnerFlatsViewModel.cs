using ApartmentManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class OwnerFlatsViewModel
    {
        [Display(Name = "Flat Number")]
        public string FlatNumber { get; set; }

        [Display(Name = "Building")]
        public string BuildingName { get; set; }

        public bool IsOccupied { get; set; }
        public Guid Id { get; set; }

        public ICollection<Tenant> Tenants { get; set; }
    }
}
