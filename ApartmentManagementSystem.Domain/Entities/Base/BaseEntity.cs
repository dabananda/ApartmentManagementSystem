using System.ComponentModel.DataAnnotations;

using ApartmentManagementSystem.Domain.Common;

namespace ApartmentManagementSystem.Domain.Entities.Base;

public abstract class BaseEntity : Entity, IAuditableEntity, ISoftDeletable
{

    public DateTime CreatedAt { get; set; }

    [StringLength(100)]
    public string? CreatedBy { get; set; }

    public DateTime? UpdatedAt { get; set; }

    [StringLength(100)]
    public string? UpdatedBy { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime? DeletedAt { get; set; }
}
