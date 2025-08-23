using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ApartmentManagementSystem.Models
{
    public class CommonExpense
    {
        public Guid Id { get; set; }

        [Required]
        [Display(Name = "Expense Name")]
        [StringLength(100)]
        public string Name { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Expense Date")]
        public DateTime ExpenseDate { get; set; } = DateTime.Today;

        [Required]
        [DataType(DataType.Currency)]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }

        public string? Notes { get; set; }

        public Guid BuildingId { get; set; }
        [ForeignKey("BuildingId")]
        public virtual Building? Building { get; set; }
    }
}
