using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using AMS.Domain.Common;
using AMS.Domain.Entities.Base;

namespace AMS.Domain.Entities;

public class Flat : BaseEntity, IAggregateRoot
{
    [Required]
    [StringLength(50)]
    public string FlatNumber { get; private set; } = default!;

    public Guid BuildingId { get; private set; }
    public virtual Building? Building { get; private set; }

    public string? OwnerId { get; private set; }
    [ForeignKey("OwnerId")]
    public virtual ApplicationUser? Owner { get; private set; }

    public bool IsOccupied { get; private set; } = false;

    public ICollection<Tenant> Tenants { get; private set; } = new List<Tenant>();

    protected Flat() { } // For EF Core

    public static Flat Create(string flatNumber, Guid buildingId, string? ownerId = null)
    {
        return new Flat
        {
            Id = Guid.NewGuid(),
            FlatNumber = flatNumber,
            BuildingId = buildingId,
            OwnerId = ownerId
        };
    }

    public void UpdateDetails(string flatNumber, Guid buildingId)
    {
        FlatNumber = flatNumber;
        BuildingId = buildingId;
    }

    public void AssignOwner(string? ownerId)
    {
        OwnerId = ownerId;
    }

    public void MarkAsOccupied()
    {
        IsOccupied = true;
    }

    public void MarkAsVacant()
    {
        IsOccupied = false;
    }
}
