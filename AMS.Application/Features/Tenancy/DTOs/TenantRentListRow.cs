namespace AMS.Application.Features.Tenancy.DTOs;

public class TenantRentListRow
{
    public string TenantUserId { get; set; } = default!;
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid BuildingId { get; set; }
}
