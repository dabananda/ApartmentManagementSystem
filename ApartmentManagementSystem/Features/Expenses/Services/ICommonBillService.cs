using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;

namespace ApartmentManagementSystem.Features.Expenses.Services;

public interface ICommonBillService
{
    Task<IReadOnlyList<CommonBill>> GetForBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task CreateAsync(CommonBill bill, CancellationToken cancellationToken = default);
    Task<CommonBill?> GetAsync(Guid id, bool includeBuilding = false, CancellationToken cancellationToken = default);
    Task UpdateAsync(CommonBill bill, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken = default);
    Task<bool> HasPaymentsAsync(Guid billId, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommonBill bill, CancellationToken cancellationToken = default);
}
