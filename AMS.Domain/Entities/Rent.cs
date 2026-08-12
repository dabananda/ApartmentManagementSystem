using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities
{
    public class Rent
    {
        public Guid Id { get; set; }
        [Required]
        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;
        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
        public string? Notes { get; set; }
        public Guid TenantId { get; set; }
        [ForeignKey("TenantId")]
        public virtual Tenant? Tenant { get; set; }

        public Guid? TenantBillId { get; set; }
        [ForeignKey(nameof(TenantBillId))] public TenantBill? TenantBill { get; set; }
    }
}
