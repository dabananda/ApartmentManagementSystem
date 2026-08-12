using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Common;
using AMS.Domain.Entities.Base;

namespace AMS.Domain.Entities
{
    public class Tenant : BaseEntity, IAggregateRoot
    {
        [Required]
        [StringLength(100)]
        public string Fullname { get; set; } = default!;
        [EmailAddress]
        public string? Email { get; set; }
        [Required]
        [StringLength(20)]
        [Display(Name = "Phone Number")]
        public string PhoneNumber { get; set; } = default!;
        public bool IsActive { get; set; } = true;
        public Guid FlatId { get; set; }
        [ForeignKey("FlatId")]
        public virtual Flat? Flat { get; set; }
        public ICollection<Rent>? Rents { get; set; } = new List<Rent>();

        public string? UserId { get; set; }
        [ForeignKey("UserId")]
        public ApplicationUser? User { get; set; }
    }
}
