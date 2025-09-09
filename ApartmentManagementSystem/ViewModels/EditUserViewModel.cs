using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class EditUserViewModel
    {
        [Required]
        public string Id { get; set; } = default!;

        [Display(Name = "Full name"), Required, MaxLength(100)]
        public string Fullname { get; set; } = default!;

        [EmailAddress, Display(Name = "Email (read-only)")]
        public string? Email { get; set; }

        [Phone, Display(Name = "Phone")]
        public string? PhoneNumber { get; set; }

        [Display(Name = "Building")]
        public Guid? BuildingId { get; set; }

        // display-only helpers
        public string? BuildingName { get; set; }
        public bool IsSuperAdminCaller { get; set; } = false;
    }
}
