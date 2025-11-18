using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels.Admin
{
    public class CreateUserViewModel
    {
        [Required, MaxLength(100)]
        public string Fullname { get; set; } = default!;

        [Required, EmailAddress]
        public string Email { get; set; } = default!;

        [Phone, Display(Name = "Phone Number")]
        public string? PhoneNumber { get; set; }

        [Required, Display(Name = "Building")]
        public Guid BuildingId { get; set; }

        [Required, StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = default!;

        [DataType(DataType.Password), Display(Name = "Confirm Password")]
        [Compare("Password")]
        public string ConfirmPassword { get; set; } = default!;

        [Required]
        public string Role { get; set; } = "User";
    }
}
