using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ApartmentManagementSystem.ViewModels
{
    public class AssignOwnerViewModel
    {
        [Required]
        public string OwnerId { get; set; }
        [Required]
        public Guid FlatId { get; set; }
        public SelectList Owners { get; set; }
        public SelectList Flats { get; set; }
    }
}