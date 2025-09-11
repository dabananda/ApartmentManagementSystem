using ApartmentManagementSystem.Models;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels.Owner
{
    public class OwnerFlatsViewModel
    {
        public Guid Id { get; set; }
        public string FlatNumber { get; set; } = "";
        public string BuildingName { get; set; } = "";
        public bool IsOccupied { get; set; }
        public List<TenantRow> Tenants { get; set; } = new();

        public class TenantRow
        {
            public Guid Id { get; set; }
            public string Fullname { get; set; } = "";
            public string PhoneNumber { get; set; } = "";
            public bool IsActive { get; set; }
        }
    }
}
