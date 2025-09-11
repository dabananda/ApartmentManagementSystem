using System;
namespace ApartmentManagementSystem.ViewModels.Building
{
    public class BuildingTenantRow
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";
        public string TenantUserId { get; set; } = ""; // empty for legacy tenants without Identity user
        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string OwnerName { get; set; } = "";
        public bool IsActive { get; set; }
        public string Source { get; set; } = "Assignment"; // "Assignment" | "Legacy"
    }
}
