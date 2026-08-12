using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace AMS.Domain.Entities;

public class ApplicationUser : IdentityUser
{
    [Required, MaxLength(100)]
    public string Fullname { get; set; } = default!;
    public string? ProfilePictureUrl { get; set; }
    public Guid? BuildingId { get; set; }
    public Building? Building { get; set; }
    public bool IsApproved { get; set; } = false;
    public string? ApprovedByUserId { get; set; }
    public DateTime? ApprovedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ICollection<Flat> OwnedFlats { get; set; } = new List<Flat>();
}
