using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class Tenant
    {
        public Guid Id { get; set; }
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; }
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; }
        public bool IsActive { get; set; } = true;
        public Guid FlatId { get; set; }
        [ForeignKey("FlatId")]
        public virtual Flat? Flat { get; set; }
        public ICollection<Rent> Rents { get; set; } = new List<Rent>();
    }
}
