namespace AMS.Application.Features.Flats.DTOs;

public class FlatTenantRow
{
    public Guid FlatId { get; set; }
    public string FlatNumber { get; set; } = "";

    public string? TenantUserId { get; set; }

    public Guid? LegacyTenantId { get; set; }

    public string TenantName { get; set; } = "";
    public string Email { get; set; } = "";
    public string? PhoneNumber { get; set; }

    public bool IsActive { get; set; }
    public string Source { get; set; } = "Assignment";
}
