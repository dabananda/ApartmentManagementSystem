using ApartmentManagementSystem.Domain.Entities;

using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.TenantPortal.DTOs;

namespace ApartmentManagementSystem.Application.Features.TenantPortal.Services;

public interface ITenantPortalService
{
    Task<(TenantAssignment? assignment, ApplicationUser? me)> GetActiveAssignmentAsync(string tenantUserId, CancellationToken cancellationToken = default);

    Task<TenantDashboardVM?> GetDashboardDataAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<List<TenantBillRow>> GetBillsAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<List<TenantPaymentRow>> GetPaymentsAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<List<Announcement>> GetNoticesAsync(Guid? buildingId, CancellationToken cancellationToken = default);

    Task<List<MaintenanceTicket>> GetTicketsAsync(Guid buildingId, Guid flatId, string tenantUserId, CancellationToken cancellationToken = default);
    Task CreateTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);

    Task<List<EntryLog>> GetVisitorsAsync(Guid buildingId, Guid flatId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}
