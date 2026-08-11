using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Owner.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;

namespace ApartmentManagementSystem.Features.Owner.Services;

public interface IOwnerBillingService
{
    Task<IReadOnlyList<OwnerBillingRow>> GetIndexRowsAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<OwnerBillsPage?> GetBillsPageAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    
    // Receipt fetching
    Task<(ExpenseAllocationPayment? payment, Guid? buildingId)> GetReceiptDataAsync(Guid paymentId, CancellationToken cancellationToken = default);

    // Payments
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAsync(string ownerId, Guid commonBillId, RecordOwnerPaymentVM vm, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> PayAllAsync(string ownerId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message, List<ExpenseAllocationPayment> payments)> FullPayAsync(string ownerId, Guid commonBillId, Guid? restrictToBuildingId, CancellationToken cancellationToken = default);
}
