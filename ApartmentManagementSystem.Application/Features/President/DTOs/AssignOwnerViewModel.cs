using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.Application.Features.President.DTOs
{
    public class AssignOwnerViewModel
    {
        public string? OwnerId { get; set; }
        [Required]
        public Guid FlatId { get; set; }
        public SelectList? Owners { get; set; }
        public SelectList? Flats { get; set; }
    }
}
