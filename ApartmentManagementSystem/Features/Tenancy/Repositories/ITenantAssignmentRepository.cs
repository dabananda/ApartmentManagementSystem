using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;
namespace ApartmentManagementSystem.Features.Tenancy.Repositories;
public interface ITenantAssignmentRepository { Task<IReadOnlyList<Flat>> GetFlatsAsync(string? ownerId); Task<IReadOnlyList<ApplicationUser>> GetAvailableTenantsAsync(); Task<Flat?> GetFlatAsync(Guid id); Task<ApplicationUser?> GetUserAsync(string id); Task<TenantAssignment?> GetActiveForTenantAsync(string id); Task<IReadOnlyList<MyTenantRow>> GetActiveRowsAsync(string? ownerId); Task ReplaceAsync(Guid flatId, string tenantId); }
