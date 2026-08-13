using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities;

public class OwnerBillingProfile
{
    public Guid Id { get; set; }

    [Required]
    public Guid FlatId { get; set; }
    [ForeignKey(nameof(FlatId))] public Flat? Flat { get; set; }

    [Column(TypeName = "decimal(18,2)")] public decimal RentAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ElectricityAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal GasAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal WaterAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal CommonBillAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal ServiceChargeAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal OtherAmount { get; set; }

    [StringLength(2000)] public string? Notes { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
