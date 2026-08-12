using AMS.Domain.Entities;
namespace AMS.Application.Interfaces.Reports;

public interface IPresidentVisitorReportRepository
{
    Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
