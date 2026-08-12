using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

using ApartmentManagementSystem.Domain.Entities.Base;

namespace ApartmentManagementSystem.Domain.Entities
{
    public class TenantBill : BaseEntity
    {

        [Required] public Guid FlatId { get; set; }
        [ForeignKey(nameof(FlatId))] public Flat? Flat { get; set; }

        [Required] public string TenantUserId { get; set; } = default!;
        [ForeignKey(nameof(TenantUserId))] public ApplicationUser? TenantUser { get; set; }

        [Required, StringLength(80)]
        public string Title { get; set; } = "Monthly Rent";

        [DataType(DataType.Date)]
        public DateTime BillDate { get; set; } = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [Timestamp] public byte[]? RowVersion { get; set; }

        public ICollection<TenantPayment> Payments { get; set; } = new List<TenantPayment>();

        [NotMapped] public decimal Paid => Payments?.Sum(p => p.Amount) ?? 0m;
        [NotMapped] public decimal Due => Amount - Paid;
        [NotMapped] public bool IsPaid => Due <= 0;
    }
}
