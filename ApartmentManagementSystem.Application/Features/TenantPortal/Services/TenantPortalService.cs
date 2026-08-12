using ApartmentManagementSystem.Application.Interfaces.TenantPortal;
using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.TenantPortal.DTOs;

namespace ApartmentManagementSystem.Application.Features.TenantPortal.Services;

public sealed class TenantPortalService(ITenantPortalRepository repository) : ITenantPortalService
{
    public Task<(TenantAssignment? assignment, ApplicationUser? me)> GetActiveAssignmentAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetActiveAssignmentAsync(tenantUserId, cancellationToken);

    public Task<TenantDashboardVM?> GetDashboardDataAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetDashboardDataAsync(tenantUserId, cancellationToken);

    public Task<List<TenantBillRow>> GetBillsAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetBillsAsync(tenantUserId, cancellationToken);

    public Task<List<TenantPaymentRow>> GetPaymentsAsync(string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetPaymentsAsync(tenantUserId, cancellationToken);

    public Task<List<Announcement>> GetNoticesAsync(Guid? buildingId, CancellationToken cancellationToken = default) =>
        repository.GetNoticesAsync(buildingId, cancellationToken);

    public Task<List<MaintenanceTicket>> GetTicketsAsync(Guid buildingId, Guid flatId, string tenantUserId, CancellationToken cancellationToken = default) =>
        repository.GetTicketsAsync(buildingId, flatId, tenantUserId, cancellationToken);

    public Task CreateTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default) =>
        repository.CreateTicketAsync(ticket, cancellationToken);

    public Task<List<EntryLog>> GetVisitorsAsync(Guid buildingId, Guid flatId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default) =>
        repository.GetVisitorsAsync(buildingId, flatId, from, to, cancellationToken);
}
