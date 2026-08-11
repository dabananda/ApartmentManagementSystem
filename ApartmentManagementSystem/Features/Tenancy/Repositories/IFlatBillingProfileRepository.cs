using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Flats.ViewModels;

namespace ApartmentManagementSystem.Features.Tenancy.Repositories;

public interface IFlatBillingProfileRepository
{
    Task<IReadOnlyList<FlatProfileRow>> GetRowsAsync(string? ownerId, CancellationToken cancellationToken = default);
    Task<Flat?> GetFlatAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task<FlatBillingProfile?> GetProfileAsync(Guid flatId, CancellationToken cancellationToken = default);
    Task SaveProfileAsync(FlatBillingProfile profile, CancellationToken cancellationToken = default);
    Task<TenantAssignment?> GetCurrentAssignmentAsync(Guid flatId, DateTime today, CancellationToken cancellationToken = default);
    Task<bool> TenantBillExistsAsync(Guid flatId, string tenantUserId, DateTime billDate, CancellationToken cancellationToken = default);
    Task AddTenantBillAsync(TenantBill bill, CancellationToken cancellationToken = default);
}
