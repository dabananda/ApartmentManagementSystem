namespace AMS.Application.Features.Owner.DTOs;

public class OwnerAllocationRowViewModel
{
    public string OwnerId { get; set; } = default!;
    public string OwnerName { get; set; } = default!;
    public string FlatsCsv { get; set; } = string.Empty;

    public decimal TotalAllocated { get; set; }
    public decimal TotalPaid { get; set; }
    public decimal TotalDue => TotalAllocated - TotalPaid;

    public Guid CommonBillId { get; set; }
}
