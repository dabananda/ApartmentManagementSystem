using ApartmentManagementSystem.Application.Interfaces.Reports;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using Microsoft.EntityFrameworkCore;
namespace ApartmentManagementSystem.Infrastructure.Repositories.Reports;

public sealed class PresidentVisitorReportRepository(ApplicationDbContext context) : IPresidentVisitorReportRepository
{
    public async Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default) => await context.EntryLogs.AsNoTracking().Where(entry => entry.BuildingId == buildingId && entry.EntryTime >= start && entry.EntryTime < endExclusive).OrderBy(entry => entry.EntryTime).ToListAsync(cancellationToken);
}
