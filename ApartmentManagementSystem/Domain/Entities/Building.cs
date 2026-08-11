using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class Building
    {
        public Guid Id { get; set; }

        [Required, StringLength(100)]
        public string Name { get; set; } = default!;

        [StringLength(255)]
        public string? Address { get; set; }

        [Required, MaxLength(16)]
        public string Code { get; set; } = default!;

        public ICollection<Flat>? Flats { get; set; }
        public ICollection<CommonBill>? CommonBills { get; set; } = new List<CommonBill>();
        public ICollection<ExpensePayment>? ExpensePayments { get; set; } = new List<ExpensePayment>();
        public ICollection<EntryLog> EntryLogs { get; set; } = new List<EntryLog>();
    }
}
