namespace ApartmentManagementSystem.Domain.Entities.Base;

public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
}
