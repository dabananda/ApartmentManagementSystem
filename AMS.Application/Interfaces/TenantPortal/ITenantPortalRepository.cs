using AMS.Application.Features.TenantPortal.DTOs;
using AMS.Domain.Entities;

namespace AMS.Application.Interfaces.TenantPortal;

public interface ITenantPortalRepository
{
    Task<(TenantAssignment? assignment, ApplicationUser? me)> GetActiveAssignmentAsync(string tenantUserId, CancellationToken cancellationToken = default);

    Task<TenantDashboardVM?> GetDashboardDataAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantBillRow>> GetBillsAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<TenantPaymentRow>> GetPaymentsAsync(string tenantUserId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Announcement>> GetNoticesAsync(Guid? buildingId, CancellationToken cancellationToken = default);
    Task<IEnumerable<MaintenanceTicket>> GetTicketsAsync(Guid buildingId, Guid flatId, string tenantUserId, CancellationToken cancellationToken = default);
    Task CreateTicketAsync(MaintenanceTicket ticket, CancellationToken cancellationToken = default);
    Task<IEnumerable<EntryLog>> GetVisitorsAsync(Guid buildingId, Guid flatId, DateTime? from, DateTime? to, CancellationToken cancellationToken = default);
}

