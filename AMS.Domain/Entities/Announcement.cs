using System.ComponentModel.DataAnnotations;

using AMS.Domain.Entities.Base;

namespace AMS.Domain.Entities
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
