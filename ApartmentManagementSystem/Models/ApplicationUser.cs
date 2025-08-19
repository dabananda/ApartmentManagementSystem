using Microsoft.AspNetCore.Identity;

namespace ApartmentManagementSystem.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string Fullname { get; set; }
        public Guid? BuildingId { get; set; }
    }
}
