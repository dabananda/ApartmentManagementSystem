using ApartmentManagementSystem.Application.Interfaces.Reports;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Infrastructure.Repositories.Reports;

public sealed class MaintenanceReportRepository(ApplicationDbContext context) : IMaintenanceReportRepository
{
    public async Task<MaintenanceSummary> GetSummaryAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default)
    {
        var q = context.MaintenanceTickets.AsNoTracking().Where(t => t.BuildingId == buildingId);

        var open = await q.CountAsync(t => t.Status == "Open", cancellationToken);
        var inProgress = await q.CountAsync(t => t.Status == "InProgress", cancellationToken);
        var closed = await q.CountAsync(t => t.Status == "Closed", cancellationToken);
        var createdInRange = await q.CountAsync(t => t.CreatedAt >= start && t.CreatedAt < endExclusive, cancellationToken);
        var closedInRange = await q.CountAsync(t => t.ClosedAt != null && t.ClosedAt >= start && t.ClosedAt < endExclusive, cancellationToken);

        var resolved = await q
            .Where(t => t.Status == "Closed" && t.ClosedAt != null)
            .Select(t => new { t.CreatedAt, t.ClosedAt })
            .ToListAsync(cancellationToken);

        double? avgHours = resolved.Count > 0
            ? resolved.Average(r => (r.ClosedAt!.Value - r.CreatedAt).TotalHours)
            : null;

        return new MaintenanceSummary(open, inProgress, closed, createdInRange, closedInRange, avgHours);
    }

    public async Task<IReadOnlyList<MaintenanceTicket>> GetCsvRowsAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default) =>
        await context.MaintenanceTickets.AsNoTracking()
            .Where(t => t.BuildingId == buildingId && t.CreatedAt >= start && t.CreatedAt < endExclusive)
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
}
