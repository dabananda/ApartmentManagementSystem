using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Features.Administration.ViewModels
{
    public class AssignPresidentViewModel
    {
        [Display(Name = "Building")]
        public Guid? BuildingId { get; set; }

        public List<SelectListItem> Buildings { get; set; } = new();

        [Required, Display(Name = "Owner (of selected building)")]
        public string? OwnerUserId { get; set; }

        public List<SelectListItem> Owners { get; set; } = new();
    }
}
