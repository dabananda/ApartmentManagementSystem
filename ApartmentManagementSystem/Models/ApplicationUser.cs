using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required, MaxLength(100)]
        public string Fullname { get; set; } = default!;
        public string? ProfilePictureUrl { get; set; }

        // Which building this user belongs to (if any)
        public Guid? BuildingId { get; set; }
        public Building? Building { get; set; }

        // Approval & audit
        public bool IsApproved { get; set; } = false;
        public string? ApprovedByUserId { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Ownership relation already used elsewhere
        public ICollection<Flat> OwnedFlats { get; set; } = new List<Flat>();
    }
}
