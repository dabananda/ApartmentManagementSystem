using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Tenancy.DTOs;
namespace AMS.Application.Interfaces.Tenancy;

public interface ITenantAssignmentRepository { Task<IReadOnlyList<Flat>> GetFlatsAsync(string? ownerId); Task<IReadOnlyList<ApplicationUser>> GetAvailableTenantsAsync(); Task<Flat?> GetFlatAsync(Guid id); Task<ApplicationUser?> GetUserAsync(string id); Task<TenantAssignment?> GetActiveForTenantAsync(string id); Task<IReadOnlyList<MyTenantRow>> GetActiveRowsAsync(string? ownerId); Task ReplaceAsync(Guid flatId, string tenantId); }
