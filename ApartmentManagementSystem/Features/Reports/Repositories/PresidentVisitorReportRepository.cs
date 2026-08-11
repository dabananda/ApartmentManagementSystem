using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.EntityFrameworkCore;
namespace ApartmentManagementSystem.Features.Reports.Repositories;
public sealed class PresidentVisitorReportRepository(ApplicationDbContext context) : IPresidentVisitorReportRepository
{
    public async Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default) => await context.EntryLogs.AsNoTracking().Where(entry => entry.BuildingId == buildingId && entry.EntryTime >= start && entry.EntryTime < endExclusive).OrderBy(entry => entry.EntryTime).ToListAsync(cancellationToken);
}
