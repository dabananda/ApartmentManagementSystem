using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class ExpenseAllocation
    {
        public Guid Id { get; set; }

        public Guid CommonBillId { get; set; }
        [ForeignKey("CommonBillId")]
        public virtual CommonBill? CommonBill { get; set; }

        public string OwnerId { get; set; } = default!;
        [ForeignKey("OwnerId")]
        public virtual ApplicationUser? Owner { get; set; }

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal AmountDue { get; set; }

        // Legacy flags retained for compatibility; we’ll derive from payments going forward
        public bool IsPaid { get; set; } = false;
        public DateTime? PaymentDate { get; set; }

        // Navigation to payments
        public ICollection<ExpenseAllocationPayment> Payments { get; set; } = new List<ExpenseAllocationPayment>();

        [NotMapped]
        public decimal AmountPaid => Payments?.Sum(p => p.Amount) ?? 0m;

        [NotMapped]
        public decimal AmountRemaining => AmountDue - AmountPaid;
    }
}
