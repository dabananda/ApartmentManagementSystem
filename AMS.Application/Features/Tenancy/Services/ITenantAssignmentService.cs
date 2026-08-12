using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Tenancy.DTOs;
namespace AMS.Application.Features.Tenancy.Services;

public interface ITenantAssignmentService { Task<AssignTenantVM> GetAssignmentFormAsync(string? ownerId); Task<Flat?> GetFlatAsync(Guid id); Task<bool> TenantExistsAsync(string id); Task<TenantAssignment?> GetActiveAssignmentAsync(string id); Task AssignAsync(Guid flatId, string tenantId); Task<IReadOnlyList<MyTenantRow>> GetActiveAsync(string? ownerId); }
