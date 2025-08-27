using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Models
{
    public enum EntryType
    {
        Visitor,
        Delivery,
        Teacher,
        Maintenance,
        Other
    }

    public class EntryLog
    {
        public Guid Id { get; set; }

        [Required(ErrorMessage = "Full name is required")]
        [StringLength(100, ErrorMessage = "Full name cannot exceed 100 characters")]
        public string Fullname { get; set; }

        [Phone(ErrorMessage = "Please enter a valid phone number")]
        public string? PhoneNumber { get; set; }

        [Required(ErrorMessage = "Please select a building")]
        public Guid BuildingId { get; set; }
        public Building Building { get; set; }

        [Required(ErrorMessage = "Please select a flat")]
        public Guid FlatId { get; set; }
        public Flat Flat { get; set; }

        [Required(ErrorMessage = "Please select an entry type")]
        public EntryType EntryType { get; set; }

        [Required(ErrorMessage = "Number of persons is required")]
        [Range(1, 50, ErrorMessage = "Number of persons must be between 1 and 50")]
        public int NumberOfPerson { get; set; } = 1;

        [Required(ErrorMessage = "Purpose is required")]
        [StringLength(500, ErrorMessage = "Purpose cannot exceed 500 characters")]
        public string Purpose { get; set; }

        [Required(ErrorMessage = "Entry time is required")]
        public DateTime EntryTime { get; set; } = DateTime.Now;

        public DateTime? ExitTime { get; set; }
    }
}