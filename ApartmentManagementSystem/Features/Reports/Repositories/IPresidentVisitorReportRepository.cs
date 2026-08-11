using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
namespace ApartmentManagementSystem.Features.Reports.Repositories;
public interface IPresidentVisitorReportRepository
{
    Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
