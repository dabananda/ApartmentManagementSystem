using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
namespace ApartmentManagementSystem.Application.Interfaces.Reports;

public interface IPresidentVisitorReportRepository
{
    Task<IReadOnlyList<EntryLog>> GetAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
