using System.ComponentModel.DataAnnotations;
using ApartmentManagementSystem.Domain.Entities;

namespace ApartmentManagementSystem.Application.Features.Flats.DTOs;

public class FlatCreateViewModel
{
    [Required, StringLength(50)]
    public string FlatNumber { get; set; } = string.Empty;

    [Required]
    public Guid BuildingId { get; set; }

    public Flat ToEntity()
    {
        return new Flat
        {
            FlatNumber = FlatNumber,
            BuildingId = BuildingId
        };
    }
}
