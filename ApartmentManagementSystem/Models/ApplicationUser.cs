using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string Fullname { get; set; }
        public Guid? BuildingId { get; set; }
        public Building? Building { get; set; }
        public ICollection<Flat> OwnedFlats { get; set; } = new List<Flat>();
    }
}
