using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels.Owner
{
    public class CreateTenantVM
    {
        [Required, StringLength(100)]
        public string Fullname { get; set; } = "";

        [Required, EmailAddress]
        public string Email { get; set; } = "";

        [Required, DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6)]
        public string Password { get; set; } = "";
    }
}
