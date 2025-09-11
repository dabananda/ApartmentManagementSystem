namespace ApartmentManagementSystem.ViewModels
{
    public class FlatTenantRow
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";

        // Identity/assignment-based tenant
        public string? TenantUserId { get; set; }

        // Legacy tenant (old table)
        public Guid? LegacyTenantId { get; set; }

        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public string? PhoneNumber { get; set; }

        public bool IsActive { get; set; }           // assignments: true; legacy: from entity
        public string Source { get; set; } = "Assignment"; // "Assignment" | "Legacy"
    }
}
