using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AMS.Domain.Entities;

public class TenantAssignment
{
    public Guid Id { get; set; }

    [Required]
    public Guid FlatId { get; set; }
    [ForeignKey(nameof(FlatId))]
    public Flat? Flat { get; set; }

    [Required]
    public string TenantUserId { get; set; } = default!;
    [ForeignKey(nameof(TenantUserId))]
    public ApplicationUser? TenantUser { get; set; }

    [DataType(DataType.Date)]
    public DateTime StartDate { get; set; } = DateTime.Today;

    [DataType(DataType.Date)]
    public DateTime? EndDate { get; set; }

    [NotMapped] public bool IsActive => !EndDate.HasValue || EndDate.Value >= DateTime.Today;
}
