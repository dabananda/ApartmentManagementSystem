using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ApartmentManagementSystem.Features.Administration.Services;

public interface IUserManagementService
{
    // ---- Buildings (dropdown data) ----
    Task<List<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default);

    // ---- President assignment ----
    Task<List<SelectListItem>> GetOwnersForBuildingSelectAsync(Guid buildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message)> AssignPresidentAsync(Guid buildingId, string ownerUserId, string approvedByUserId, CancellationToken cancellationToken = default);

    // ---- Create / Edit user ----
    Task<(bool success, IEnumerable<string> errors)> CreateUserAsync(CreateUserViewModel model, string createdByUserId, CancellationToken cancellationToken = default);
    Task<(bool success, IEnumerable<string> errors)> UpdateUserAsync(EditUserViewModel model, bool callerIsSuperAdmin, CancellationToken cancellationToken = default);

    // ---- Approvals ----
    Task<ApprovalsPageViewModel> GetApprovalsPageAsync(ApprovalsFilterViewModel filter, Guid? callerBuildingId, bool callerIsSuperAdmin, CancellationToken cancellationToken = default);
    Task<(bool success, string message)> ApproveUserAsync(string userId, string role, string approvedByUserId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<int> BulkApproveAsync(string[] ids, string role, string approvedByUserId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message)> ResetUserAsync(string userId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);

    // ---- Manage users ----
    Task<ManageUsersPageViewModel> GetUsersPageAsync(ManageUsersFilterViewModel filter, Guid? callerBuildingId, bool callerIsSuperAdmin, CancellationToken cancellationToken = default);
    Task<(bool success, string message)> ChangeRoleAsync(string userId, string role, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<int> BulkChangeRoleAsync(string[] ids, string role, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);

    // ---- Block / Unblock ----
    Task<(bool success, string message)> BlockUserAsync(string userId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<int> BulkBlockAsync(string[] ids, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<(bool success, string message)> UnblockUserAsync(string userId, Guid? callerBuildingId, CancellationToken cancellationToken = default);
    Task<int> BulkUnblockAsync(string[] ids, Guid? callerBuildingId, CancellationToken cancellationToken = default);

    // ---- Delete ----
    Task<(bool success, string message)> DeleteUserAsync(string userId, CancellationToken cancellationToken = default);
    Task<int> BulkDeleteAsync(string[] ids, CancellationToken cancellationToken = default);
}
