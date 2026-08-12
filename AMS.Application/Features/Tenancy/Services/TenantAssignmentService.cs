using AMS.Application.Interfaces.Tenancy;
using AMS.Domain.Entities;

using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Tenancy.DTOs;
namespace AMS.Application.Features.Tenancy.Services;

public sealed class TenantAssignmentService(ITenantAssignmentRepository repo) : ITenantAssignmentService { public async Task<AssignTenantVM> GetAssignmentFormAsync(string? o) => new() { Flats = (await repo.GetFlatsAsync(o)).ToList(), Tenants = (await repo.GetAvailableTenantsAsync()).ToList() }; public Task<Flat?> GetFlatAsync(Guid id) => repo.GetFlatAsync(id); public async Task<bool> TenantExistsAsync(string id) => (await repo.GetUserAsync(id)) != null; public Task<TenantAssignment?> GetActiveAssignmentAsync(string id) => repo.GetActiveForTenantAsync(id); public Task AssignAsync(Guid f, string t) => repo.ReplaceAsync(f, t); public Task<IReadOnlyList<MyTenantRow>> GetActiveAsync(string? o) => repo.GetActiveRowsAsync(o); }
