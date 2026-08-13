namespace AMS.Application.Interfaces.Reports;

public interface IPresidentOccupancyReportRepository
{
    Task<(int TotalFlats, int OccupiedFlats, int OwnersCount, int TenantsCount)> GetSummaryAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<OccupancyFlatRow>> GetFlatsAsync(Guid buildingId, CancellationToken cancellationToken = default);
}
public sealed record OccupancyFlatRow(string FlatNumber, bool IsOccupied, bool HasOwner);
