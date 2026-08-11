using ApartmentManagementSystem.Models;
namespace ApartmentManagementSystem.Features.Reports.Repositories;
public interface IPresidentVisitorReportRepository
{
    Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
