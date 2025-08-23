using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class CommonBill
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Bill Name")]
        [StringLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Bill Date")]
        public DateTime BillDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }

        public string? Notes { get; set; }

        public Guid BuildingId { get; set; }
        [ForeignKey("BuildingId")]
        public virtual Building? Building { get; set; }

        public ICollection<ExpenseAllocation>? Allocations { get; set; } = new List<ExpenseAllocation>();
    }
}
