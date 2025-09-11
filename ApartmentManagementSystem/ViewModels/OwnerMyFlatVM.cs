namespace ApartmentManagementSystem.ViewModels
{
    public class OwnerMyFlatVM
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";
        public string? CurrentTenantUserId { get; set; }
        public string? CurrentTenantName { get; set; }
        public string? CurrentTenantEmail { get; set; }
    }
}
