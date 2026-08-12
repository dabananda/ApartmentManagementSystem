namespace AMS.Application.Features.Flats.DTOs
{
    public class FlatProfileRow
    {
        public Guid FlatId { get; set; }
        public string FlatNumber { get; set; } = "";
        public bool HasProfile { get; set; }
        public string Title { get; set; } = "";
        public decimal Amount { get; set; }
        public int DueDay { get; set; }
        public bool IsActive { get; set; }
    }
}
