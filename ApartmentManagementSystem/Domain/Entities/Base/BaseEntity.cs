using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Domain.Entities.Base;

public abstract class BaseEntity : IAuditableEntity, ISoftDeletable
{
    [Key]
    public Guid Id { get; set; }

    public DateTime CreatedAt { get; set; }
    
    [StringLength(100)]
    public string? CreatedBy { get; set; }
    
    public DateTime? UpdatedAt { get; set; }
    
    [StringLength(100)]
    public string? UpdatedBy { get; set; }
    
    public bool IsDeleted { get; set; }
    
    public DateTime? DeletedAt { get; set; }
}
