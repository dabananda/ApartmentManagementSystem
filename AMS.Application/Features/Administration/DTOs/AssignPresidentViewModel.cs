using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Application.Features.Administration.DTOs;

public class AssignPresidentViewModel
{
    [Display(Name = "Building")]
    public Guid? BuildingId { get; set; }

    public IEnumerable<SelectListItem> Buildings { get; set; } = [];

    [Required, Display(Name = "Owner (of selected building)")]
    public string? OwnerUserId { get; set; }

    public IEnumerable<SelectListItem> Owners { get; set; } = [];
}


