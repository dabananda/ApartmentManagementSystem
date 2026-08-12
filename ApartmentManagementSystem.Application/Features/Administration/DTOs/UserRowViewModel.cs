namespace ApartmentManagementSystem.Application.Features.Administration.DTOs
{
    public class UserRowViewModel
    {
        public string Id { get; set; } = default!;
        public string Fullname { get; set; } = default!;
        public string? Email { get; set; }
        public string? PhoneNumber { get; set; }

        public Guid? BuildingId { get; set; }
        public string? BuildingName { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool IsApproved { get; set; }
        public bool IsLockedOut { get; set; }
        public bool IsPresident { get; set; }

        public DateTime CreatedAt { get; set; }

        public List<string> Roles { get; set; } = new();
    }
}
