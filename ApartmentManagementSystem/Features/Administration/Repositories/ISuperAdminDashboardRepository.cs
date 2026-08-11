using ApartmentManagementSystem.Models;

namespace ApartmentManagementSystem.Features.Administration.Repositories;

public interface ISuperAdminDashboardRepository
{
    Task<IReadOnlyList<Building>> GetBuildingsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlySet<Guid>> GetOccupiedFlatIdsAsync(CancellationToken cancellationToken = default);
    Task<(int TotalFlats, int FlatsWithOwners)> GetFlatCountsAsync(CancellationToken cancellationToken = default);
    Task<(decimal Bills, decimal Payments, decimal Collected, decimal Allocated)> GetFinancialTotalsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CommonBill>> GetRecentBillsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ExpensePayment>> GetRecentPaymentsAsync(CancellationToken cancellationToken = default);
}
