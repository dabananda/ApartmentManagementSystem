using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class RegisterUserViewModel
    {
        [Required]
        [Display(Name = "Full Name")]
        public string Fullname { get; set; }
        [Required]
        [EmailAddress]
        public string Email { get; set; }
        [Required]
        [StringLength(100, ErrorMessage = "The {0} must be at least {2} and at max {1} characters long.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
        [DataType(DataType.Password)]
        [Display(Name = "Confirm password")]
        [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
        public string ConfirmPassword { get; set; }
        [Required]
        [Display(Name = "Role")]
        public string SelectedRole { get; set; }
    }
}
