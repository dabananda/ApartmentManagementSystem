using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class AssignPresidentViewModel
    {
        [Required]
        [Display(Name = "Select User")]
        public string UserId { get; set; }

        [Required]
        [Display(Name = "Select Building")]
        public Guid BuildingId { get; set; }

        // These properties will hold the data for the dropdowns
        public SelectList? Users { get; set; }
        public SelectList? Buildings { get; set; }
    }
}
