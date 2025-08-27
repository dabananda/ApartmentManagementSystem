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
        public string Fullname { get; set; }
        public string? PhoneNumber { get; set; }
        public Guid BuildingId { get; set; }
        public Building Building { get; set; }
        public Guid FlatId { get; set; }
        public Flat Flat { get; set; }
        public EntryType EntryType { get; set; }
        public int NumberOfPerson { get; set; }
        public string Purpose { get; set; }
        public DateTime EntryTime { get; set; } = DateTime.Now;
        public DateTime? ExitTime { get; set; }
    }
}
