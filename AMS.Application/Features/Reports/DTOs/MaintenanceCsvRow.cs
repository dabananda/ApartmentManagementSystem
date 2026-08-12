namespace AMS.Application.Features.Reports.DTOs;
public sealed record MaintenanceCsvRow(string Title, string Status, DateTime CreatedAt, DateTime? ClosedAt);
