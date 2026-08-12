using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Home.DTOs;
using ApartmentManagementSystem.Application.Features.Tenancy.DTOs;
namespace ApartmentManagementSystem.Application.Interfaces.Tenancy;

public interface ITenantAssignmentRepository { Task<IReadOnlyList<Flat>> GetFlatsAsync(string? ownerId); Task<IReadOnlyList<ApplicationUser>> GetAvailableTenantsAsync(); Task<Flat?> GetFlatAsync(Guid id); Task<ApplicationUser?> GetUserAsync(string id); Task<TenantAssignment?> GetActiveForTenantAsync(string id); Task<IReadOnlyList<MyTenantRow>> GetActiveRowsAsync(string? ownerId); Task ReplaceAsync(Guid flatId, string tenantId); }
