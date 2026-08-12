using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Features.Tenancy.DTOs
{
    public class AssignTenantVM
    {
        [Required] public Guid FlatId { get; set; }
        [Required] public string TenantUserId { get; set; } = default!;

        public List<Flat> Flats { get; set; } = new();
        public List<ApplicationUser> Tenants { get; set; } = new();
    }

    public class MyTenantRow
    {
        public string TenantUserId { get; set; } = default!;
        public string TenantName { get; set; } = "";
        public string Email { get; set; } = "";
        public string FlatNumber { get; set; } = "";
        public DateTime From { get; set; }
    }
}
