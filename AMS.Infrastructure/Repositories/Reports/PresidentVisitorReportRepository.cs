using AMS.Application.Interfaces.Reports;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;
namespace AMS.Infrastructure.Repositories.Reports;

public sealed class PresidentVisitorReportRepository(ApplicationDbContext context) : IPresidentVisitorReportRepository
{
    public async Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default) => await context.EntryLogs.AsNoTracking().Where(entry => entry.BuildingId == buildingId && entry.EntryTime >= start && entry.EntryTime < endExclusive).OrderBy(entry => entry.EntryTime).ToListAsync(cancellationToken);
}
