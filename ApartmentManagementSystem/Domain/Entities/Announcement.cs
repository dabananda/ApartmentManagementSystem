using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class Announcement
    {
        public Guid Id { get; set; }

        [Required]
        public Guid BuildingId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = "";

        [Required, StringLength(2000)]
        public string Body { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
