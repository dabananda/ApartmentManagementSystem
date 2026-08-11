namespace ApartmentManagementSystem.Features.Buildings.ViewModels
{
    public class BuildingTenantRow
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";
        public string TenantUserId { get; set; } = "";
        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }
        public string OwnerName { get; set; } = "";
        public bool IsActive { get; set; }
        public string Source { get; set; } = "Assignment";
    }
}
