using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
namespace AMS.Application.Interfaces.Reports;

public interface IPresidentVisitorReportRepository
{
    Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
