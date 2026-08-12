using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class FlatBillingProfile
    {
        public Guid Id { get; set; }

        [Required]
        public Guid FlatId { get; set; }
        [ForeignKey(nameof(FlatId))]
        public Flat? Flat { get; set; }

        [Required, StringLength(80)]
        public string Title { get; set; } = "Monthly Rent";

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal MonthlyAmount { get; set; }

        [Range(0, 31)]
        public int DueDayOfMonth { get; set; } = 1;
        public bool IsActive { get; set; } = true;
    }
}
