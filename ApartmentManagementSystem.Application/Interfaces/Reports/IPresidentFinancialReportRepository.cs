using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;

namespace ApartmentManagementSystem.Application.Interfaces.Reports;

public interface IPresidentFinancialReportRepository
{
    Task<IReadOnlyList<CommonBill>> GetBillsAsync(Guid buildingId, DateTime start, DateTime endExclusive, bool descending, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetSucceededCollectionsAsync(IReadOnlyCollection<Guid> billIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, decimal>> GetPaidAllocationCollectionsAsync(IReadOnlyCollection<Guid> billIds, CancellationToken cancellationToken = default);
    Task<decimal> GetPaymentsTotalAsync(Guid buildingId, DateTime start, DateTime endExclusive, CancellationToken cancellationToken = default);
}
