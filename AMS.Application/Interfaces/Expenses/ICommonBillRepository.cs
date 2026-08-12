using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;

namespace AMS.Application.Interfaces.Expenses;

public interface ICommonBillRepository
{
    Task<IReadOnlyList<CommonBill>> GetByBuildingAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<CommonBill?> GetAsync(Guid id, bool includeBuilding = false, CancellationToken cancellationToken = default);
    Task AddAsync(CommonBill bill, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApplicationUser>> GetBuildingOwnersAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<int> CountOwnerFlatsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<int> CountOwnerFlatsAsync(string ownerId, CancellationToken cancellationToken = default);
    Task AddAllocationAsync(ExpenseAllocation allocation, CancellationToken cancellationToken = default);
    Task<bool> HasPaymentsAsync(Guid billId, CancellationToken cancellationToken = default);
    Task DeleteAsync(CommonBill bill, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
