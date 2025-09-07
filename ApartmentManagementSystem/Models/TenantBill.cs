using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class TenantBill
    {
        public Guid Id { get; set; }

        [Required] public Guid FlatId { get; set; }
        [ForeignKey(nameof(FlatId))] public Flat? Flat { get; set; }

        [Required] public Guid TenantId { get; set; }
        [ForeignKey(nameof(TenantId))] public Tenant? Tenant { get; set; }

        [Range(2000, 3000)] public int Year { get; set; }
        [Range(1, 12)] public int Month { get; set; }

        [Column(TypeName = "decimal(18,2)")] public decimal RentAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ElectricityAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal GasAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal WaterAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal CommonBillAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal ServiceChargeAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal OtherAmount { get; set; }

        [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
        [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
        [StringLength(16)] public string Status { get; set; } = "Unpaid"; // Unpaid|PartiallyPaid|Paid

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }

        // NEW: optimistic concurrency token
        [Timestamp] public byte[]? RowVersion { get; set; }
    }
}
