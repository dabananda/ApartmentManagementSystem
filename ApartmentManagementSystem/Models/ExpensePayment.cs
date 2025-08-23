using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class ExpensePayment
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Payment Name")]
        [StringLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Payment Date")]
        public DateTime PaymentDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        public Guid CommonBillId { get; set; }
        [ForeignKey("CommonBillId")]
        public virtual CommonBill? CommonBill { get; set; }

        public Guid BuildingId { get; set; }
        [ForeignKey("BuildingId")]
        public virtual Building? Building { get; set; }
    }
}
