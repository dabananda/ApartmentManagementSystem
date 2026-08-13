using System.ComponentModel.DataAnnotations;
using AMS.Domain.Entities;

namespace AMS.Application.Features.Tenancy.DTOs;

public class AssignTenantVM
{
    [Required] public Guid FlatId { get; set; }
    [Required] public string TenantUserId { get; set; } = default!;

    public IEnumerable<Flat> Flats { get; set; } = [];
    public IEnumerable<ApplicationUser> Tenants { get; set; } = [];
}

public class MyTenantRow
{
    public string TenantUserId { get; set; } = default!;
    public string TenantName { get; set; } = "";
    public string Email { get; set; } = "";
    public string FlatNumber { get; set; } = "";
    public DateTime From { get; set; }
}


