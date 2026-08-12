using System.ComponentModel.DataAnnotations;

using AMS.Domain.Entities.Base;

using AMS.Domain.Common;

namespace AMS.Domain.Entities
{
    public class MaintenanceTicket : BaseEntity, IAggregateRoot
    {

        [Required]
        public Guid BuildingId { get; set; }

        [Required, StringLength(140)]
        public string Title { get; set; } = "";

        [Required, StringLength(2000)]
        public string Description { get; set; } = "";

        [Required, StringLength(20)]
        public string Status { get; set; } = "Open";

        public DateTime? ClosedAt { get; set; }

        public Guid? FlatId { get; set; }
        public string? CreatedByUserId { get; set; }
    }
}
