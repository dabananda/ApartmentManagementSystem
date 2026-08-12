using AMS.Application.Interfaces.Reports;
using AMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AMS.Infrastructure.Repositories.Reports;

public sealed class PresidentOccupancyReportRepository(ApplicationDbContext context) : IPresidentOccupancyReportRepository
{
    public async Task<(int TotalFlats, int OccupiedFlats, int OwnersCount, int TenantsCount)> GetSummaryAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        (await context.Flats.AsNoTracking().CountAsync(flat => flat.BuildingId == buildingId, cancellationToken), await context.Flats.AsNoTracking().CountAsync(flat => flat.BuildingId == buildingId && flat.IsOccupied, cancellationToken), await context.Flats.AsNoTracking().Where(flat => flat.BuildingId == buildingId && flat.OwnerId != null).Select(flat => flat.OwnerId).Distinct().CountAsync(cancellationToken), await (from assignment in context.TenantAssignments.AsNoTracking() join flat in context.Flats.AsNoTracking() on assignment.FlatId equals flat.Id where flat.BuildingId == buildingId && (assignment.EndDate == null || assignment.EndDate >= DateTime.Today) select assignment).CountAsync(cancellationToken));
    public async Task<IReadOnlyList<OccupancyFlatRow>> GetFlatsAsync(Guid buildingId, CancellationToken cancellationToken = default) =>
        await context.Flats.AsNoTracking().Where(flat => flat.BuildingId == buildingId).OrderBy(flat => flat.FlatNumber).Select(flat => new OccupancyFlatRow(flat.FlatNumber, flat.IsOccupied, flat.OwnerId != null)).ToListAsync(cancellationToken);
}
