using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using AMS.Domain.Entities.Base;

using AMS.Domain.Common;

namespace AMS.Domain.Entities
{
    public class Flat : BaseEntity, IAggregateRoot
    {
        [Required]
        [StringLength(50)]
        public string FlatNumber { get; set; } = default!;
        public Guid BuildingId { get; set; }
        public virtual Building? Building { get; set; }
        public string? OwnerId { get; set; }
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }
        public bool IsOccupied { get; set; } = false;
        public ICollection<Tenant> Tenants { get; set; } = new List<Tenant>();
    }
}
