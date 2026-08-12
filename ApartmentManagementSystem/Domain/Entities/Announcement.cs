using System.ComponentModel.DataAnnotations;

using ApartmentManagementSystem.Domain.Entities.Base;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class Announcement : BaseEntity
    {

        [Required]
        public Guid BuildingId { get; set; }

        [Required, StringLength(120)]
        public string Title { get; set; } = "";

        [Required, StringLength(2000)]
        public string Body { get; set; } = "";

    }
}
