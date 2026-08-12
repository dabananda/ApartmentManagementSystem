using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities.Base;
using ApartmentManagementSystem.Domain.Common;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class Building : BaseEntity, IAggregateRoot
    {
        [Required, StringLength(100)]
        public string Name { get; private set; } = default!;

        [StringLength(255)]
        public string? Address { get; private set; }

        [Required, MaxLength(16)]
        public string Code { get; private set; } = default!;

        public ICollection<Flat>? Flats { get; private set; }
        public ICollection<CommonBill>? CommonBills { get; private set; } = new List<CommonBill>();
        public ICollection<ExpensePayment>? ExpensePayments { get; private set; } = new List<ExpensePayment>();
        public ICollection<EntryLog> EntryLogs { get; private set; } = new List<EntryLog>();

        protected Building() { } // For EF Core

        public static Building Create(string name, string code, string? address = null)
        {
            var building = new Building
            {
                Id = Guid.NewGuid(),
                Name = name,
                Code = code,
                Address = address
            };
            return building;
        }

        public void UpdateDetails(string name, string code, string? address = null)
        {
            Name = name;
            Code = code;
            Address = address;
        }
    }
}
