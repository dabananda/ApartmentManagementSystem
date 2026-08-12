using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Flats.DTOs;

public class FlatCreateViewModel
{
    [Required, StringLength(50)]
    public string FlatNumber { get; set; } = string.Empty;

    [Required]
    public Guid BuildingId { get; set; }

    public Flat ToEntity()
    {
        return Flat.Create(
            flatNumber: FlatNumber,
            buildingId: BuildingId
        );
    }
}
