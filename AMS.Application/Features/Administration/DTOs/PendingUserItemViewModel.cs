namespace AMS.Application.Features.Administration.DTOs
{
    public class PendingUserItemViewModel
    {
        public string Id { get; set; } = default!;
        public string Fullname { get; set; } = default!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool IsApproved { get; set; }
        public Guid? BuildingId { get; set; }
        public string? BuildingName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string CurrentStatus { get; set; } = "Pending";
        public bool IsPresident { get; set; }
    }
}
