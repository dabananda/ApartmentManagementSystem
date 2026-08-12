namespace AMS.Application.Features.Administration.DTOs;

public class ApprovalsFilterViewModel
{
    public Guid? BuildingId { get; set; }
    public string? Query { get; set; }
    public bool OnlyEmailConfirmed { get; set; } = true;
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
