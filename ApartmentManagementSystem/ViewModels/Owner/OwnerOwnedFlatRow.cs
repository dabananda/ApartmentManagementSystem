namespace ApartmentManagementSystem.ViewModels.Owner
{
    public class OwnerOwnedFlatRow
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";
        public string? Floor { get; set; }
        public string BuildingName { get; set; } = "";
        public Guid BuildingId { get; set; }

        public string? CurrentTenantUserId { get; set; }
        public string? CurrentTenantName { get; set; }
        public string? CurrentTenantEmail { get; set; }
        public DateTime? TenantFrom { get; set; }
    }
}
