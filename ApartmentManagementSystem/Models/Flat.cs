using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class Flat
    {
        public Guid Id { get; set; }
        [Required]
        [StringLength(50)]
        public string FlatNumber { get; set; }
        public Guid BuildingId { get; set; }
        public virtual Building Building { get; set; }
        public string? OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }
        public bool IsOccupied { get; set; } = false;
    }
}
