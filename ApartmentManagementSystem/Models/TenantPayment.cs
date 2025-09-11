using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class TenantPayment
    {
        public Guid Id { get; set; }

        [Required] public Guid TenantBillId { get; set; }
        [ForeignKey(nameof(TenantBillId))] public TenantBill? TenantBill { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }

        [DataType(DataType.Date)]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [StringLength(100)]
        public string? Reference { get; set; }
    }
}
