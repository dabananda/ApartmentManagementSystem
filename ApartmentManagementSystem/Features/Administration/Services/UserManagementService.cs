using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.Administration.Repositories;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Infrastructure.Services;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration.Services;

public sealed class UserManagementService(
    UserManager<ApplicationUser> userManager,
    IUserManagementRepository repository,
    IEmailSender email) : IUserManagementService
{
    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static readonly string[] AllRoles = ["User", "Staff", "Tenant", "Owner", "President"];

    private async Task EnsureOnlyRolesAsync(ApplicationUser user, params string[] rolesToKeep)
    {
        foreach (var r in AllRoles)
            if (await userManager.IsInRoleAsync(user, r))
                await userManager.RemoveFromRoleAsync(user, r);
        foreach (var r in rolesToKeep.Distinct())
            await userManager.AddToRoleAsync(user, r);
    }

    private static bool IsValidApprovalRole(string role) =>
        role is "Owner" or "Tenant" or "Staff";

    // ─── Buildings dropdown ───────────────────────────────────────────────────

    public Task<List<SelectListItem>> GetBuildingSelectItemsAsync(Guid? restrictToBuildingId = null, CancellationToken cancellationToken = default) =>
        repository.GetBuildingSelectItemsAsync(restrictToBuildingId, cancellationToken);

    // ─── President assignment ─────────────────────────────────────────────────

    public async Task<List<SelectListItem>> GetOwnersForBuildingSelectAsync(Guid buildingId, CancellationToken cancellationToken = default)
    {
        var ownerUsers = await userManager.GetUsersInRoleAsync(Roles.Owner);
        return ownerUsers
            .Where(u => u.BuildingId == buildingId)
            .OrderBy(u => u.Fullname)
            .Select(u => new SelectListItem
            {
                Value = u.Id,
                Text = string.IsNullOrWhiteSpace(u.Fullname) ? u.Email! : $"{u.Fullname} ({u.Email})"
            })
            .ToList();
    }

    public async Task<(bool success, string message)> AssignPresidentAsync(Guid buildingId, string ownerUserId, string approvedByUserId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(ownerUserId);
        if (user == null) return (false, "Invalid owner.");
        if (user.BuildingId != buildingId) return (false, "Selected owner does not belong to the chosen building.");
        if (!await userManager.IsInRoleAsync(user, "Owner")) return (false, "Selected user is not an Owner.");

        foreach (var r in new[] { "User", "Tenant" })
            if (await userManager.IsInRoleAsync(user, r))
                await userManager.RemoveFromRoleAsync(user, r);

        if (!await userManager.IsInRoleAsync(user, "President"))
            await userManager.AddToRoleAsync(user, "President");
        if (!await userManager.IsInRoleAsync(user, "Owner"))
            await userManager.AddToRoleAsync(user, "Owner");

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByUserId = approvedByUserId;
        await userManager.UpdateAsync(user);

        return (true, $"Assigned {user.Fullname} as President.");
    }

    // ─── Create / Edit user ───────────────────────────────────────────────────

    public async Task<(bool success, IEnumerable<string> errors)> CreateUserAsync(CreateUserViewModel model, string createdByUserId, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            Fullname = model.Fullname,
            Email = model.Email,
            UserName = model.Email,
            PhoneNumber = model.PhoneNumber,
            BuildingId = model.BuildingId,
            EmailConfirmed = true,
            IsApproved = model.Role != "User",
            ApprovedAt = model.Role == "User" ? null : DateTime.UtcNow,
            ApprovedByUserId = model.Role == "User" ? null : createdByUserId,
            CreatedAt = DateTime.UtcNow
        };

        var createRes = await userManager.CreateAsync(user, model.Password);
        if (!createRes.Succeeded)
            return (false, createRes.Errors.Select(e => e.Description));

        var rolesToKeep = model.Role switch
        {
            "Tenant" => new[] { "Tenant" },
            "Owner" => new[] { "Owner" },
            "Staff" => new[] { "Staff" },
            _ => new[] { "User" }
        };
        await EnsureOnlyRolesAsync(user, rolesToKeep);

        user.IsApproved = model.Role != "User";
        user.ApprovedAt = model.Role == "User" ? null : DateTime.UtcNow;
        user.ApprovedByUserId = model.Role == "User" ? null : createdByUserId;
        await userManager.UpdateAsync(user);

        return (true, []);
    }

    public async Task<(bool success, IEnumerable<string> errors)> UpdateUserAsync(EditUserViewModel model, bool callerIsSuperAdmin, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == model.Id, cancellationToken);
        if (user == null) return (false, ["User not found."]);

        user.Fullname = model.Fullname?.Trim() ?? user.Fullname;
        user.PhoneNumber = string.IsNullOrWhiteSpace(model.PhoneNumber) ? null : model.PhoneNumber.Trim();

        if (callerIsSuperAdmin)
            user.BuildingId = model.BuildingId;

        var res = await userManager.UpdateAsync(user);
        return res.Succeeded ? (true, []) : (false, res.Errors.Select(e => e.Description));
    }

    // ─── Approvals ────────────────────────────────────────────────────────────

    public async Task<ApprovalsPageViewModel> GetApprovalsPageAsync(ApprovalsFilterViewModel filter, Guid? callerBuildingId, bool callerIsSuperAdmin, CancellationToken cancellationToken = default)
    {
        var pendingIds = (await userManager.GetUsersInRoleAsync("User")).Select(u => u.Id).ToHashSet();

        IQueryable<ApplicationUser> q = userManager.Users
            .Include(u => u.Building)
            .Where(u => pendingIds.Contains(u.Id) || !u.IsApproved);

        if (!callerIsSuperAdmin && callerBuildingId != null)
        {
            filter.BuildingId ??= callerBuildingId;
        }

        if (filter.BuildingId != null)
            q = q.Where(u => u.BuildingId == filter.BuildingId);

        if (filter.OnlyEmailConfirmed)
            q = q.Where(u => u.EmailConfirmed);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim().ToLower();
            q = q.Where(u =>
                (u.Fullname ?? "").ToLower().Contains(term) ||
                (u.Email ?? "").ToLower().Contains(term) ||
                (u.PhoneNumber ?? "").ToLower().Contains(term));
        }

        var total = await q.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var page = Math.Max(1, filter.Page);
        var users = await q.OrderBy(u => u.Fullname).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var roleMap = new Dictionary<string, IList<string>>();
        foreach (var u in users) roleMap[u.Id] = await userManager.GetRolesAsync(u);

        var buildings = callerIsSuperAdmin
            ? await repository.GetBuildingSelectItemsAsync(cancellationToken: cancellationToken)
            : callerBuildingId != null
                ? [await repository.GetBuildingSelectItemAsync(callerBuildingId.Value, cancellationToken) ?? new SelectListItem()]
                : new List<SelectListItem>();

        return new ApprovalsPageViewModel
        {
            Filter = filter,
            Buildings = buildings,
            Total = total,
            PendingUsers = users.Select(u => new PendingUserItemViewModel
            {
                Id = u.Id,
                Fullname = u.Fullname,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                EmailConfirmed = u.EmailConfirmed,
                IsApproved = u.IsApproved,
                BuildingId = u.BuildingId,
                BuildingName = u.Building?.Name,
                CreatedAt = u.CreatedAt,
                CurrentStatus = u.IsApproved ? "Approved" : "Pending",
                IsPresident = roleMap[u.Id].Contains("President")
            }).ToList()
        };
    }

    public async Task<(bool success, string message)> ApproveUserAsync(string userId, string role, string approvedByUserId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        if (!IsValidApprovalRole(role)) return (false, "Invalid role.");

        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, "President");

        if (targetIsPresident)
        {
            if (callerIsSuperAdmin)
            {
                await EnsureOnlyRolesAsync(user, role);
            }
            else
            {
                if (role == "Tenant") return (false, "A President cannot be assigned the Tenant role.");
                if (!await userManager.IsInRoleAsync(user, "President"))
                    await userManager.AddToRoleAsync(user, "President");
                if (!await userManager.IsInRoleAsync(user, "Owner"))
                    await userManager.AddToRoleAsync(user, "Owner");
            }
        }
        else
        {
            foreach (var r in new[] { "User", "Staff", "Owner", "Tenant" })
                if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
            await userManager.AddToRoleAsync(user, role);
        }

        user.IsApproved = true;
        user.ApprovedAt = DateTime.UtcNow;
        user.ApprovedByUserId = approvedByUserId;
        await userManager.UpdateAsync(user);

        if (!string.IsNullOrWhiteSpace(user.Email))
        {
            try
            {
                var roleText = targetIsPresident && !callerIsSuperAdmin ? "President + " + role : role;
                await email.SendEmailAsync(user.Email,
                    "Your account has been approved",
                    $@"<p>Hi {user.Fullname},</p><p>Your role is now <strong>{roleText}</strong>. You can log in now.</p>");
            }
            catch { /* email failure must not block the approval */ }
        }

        var displayRole = targetIsPresident && !callerIsSuperAdmin ? "President + " + role : role;
        return (true, $"Approved {user.Fullname} as {displayRole}.");
    }

    public async Task<int> BulkApproveAsync(string[] ids, string role, string approvedByUserId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        if (!IsValidApprovalRole(role)) return 0;

        var users = await userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        int applied = 0;

        foreach (var user in users)
        {
            if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
                continue;

            var targetIsPresident = await userManager.IsInRoleAsync(user, "President");
            if (targetIsPresident)
            {
                if (callerIsSuperAdmin)
                {
                    await EnsureOnlyRolesAsync(user, role);
                }
                else
                {
                    if (role == "Tenant") continue;
                    if (!await userManager.IsInRoleAsync(user, "President"))
                        await userManager.AddToRoleAsync(user, "President");
                    if (!await userManager.IsInRoleAsync(user, "Owner"))
                        await userManager.AddToRoleAsync(user, "Owner");
                }
            }
            else
            {
                foreach (var r in new[] { "User", "Staff", "Owner", "Tenant" })
                    if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
                await userManager.AddToRoleAsync(user, role);
            }

            user.IsApproved = true;
            user.ApprovedAt = DateTime.UtcNow;
            user.ApprovedByUserId = approvedByUserId;
            await userManager.UpdateAsync(user);
            applied++;
        }

        return applied;
    }

    public async Task<(bool success, string message)> ResetUserAsync(string userId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, "President");
        if (targetIsPresident && !callerIsSuperAdmin)
            return (false, "Only SuperAdmin can reset a President.");

        foreach (var r in new[] { "Owner", "Tenant", "President", "User" })
            if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
        await userManager.AddToRoleAsync(user, "User");

        user.IsApproved = false;
        user.ApprovedAt = null;
        user.ApprovedByUserId = null;
        await userManager.UpdateAsync(user);

        return (true, $"Reset {user.Fullname} to pending.");
    }

    // ─── Manage users ─────────────────────────────────────────────────────────

    public async Task<ManageUsersPageViewModel> GetUsersPageAsync(ManageUsersFilterViewModel filter, Guid? callerBuildingId, bool callerIsSuperAdmin, CancellationToken cancellationToken = default)
    {
        IQueryable<ApplicationUser> q = userManager.Users.Where(u => u.IsApproved).Include(u => u.Building);

        if (!callerIsSuperAdmin && callerBuildingId != null)
            filter.BuildingId ??= callerBuildingId;

        if (filter.BuildingId.HasValue)
            q = q.Where(u => u.BuildingId == filter.BuildingId);

        if (filter.LockedOnly)
            q = q.Where(u => u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow);

        if (!string.IsNullOrWhiteSpace(filter.Query))
        {
            var term = filter.Query.Trim().ToLower();
            q = q.Where(u =>
                (u.Fullname ?? "").ToLower().Contains(term) ||
                (u.Email ?? "").ToLower().Contains(term) ||
                (u.PhoneNumber ?? "").ToLower().Contains(term));
        }

        if (!string.Equals(filter.Role, "All", StringComparison.OrdinalIgnoreCase))
        {
            var roleUsers = await userManager.GetUsersInRoleAsync(filter.Role);
            var ids = roleUsers.Select(u => u.Id).ToList();
            q = q.Where(u => ids.Contains(u.Id));
        }

        var total = await q.CountAsync(cancellationToken);
        var pageSize = Math.Clamp(filter.PageSize, 5, 100);
        var page = Math.Max(1, filter.Page);
        var users = await q.OrderBy(u => u.Fullname).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);

        var roleMap = new Dictionary<string, IList<string>>();
        foreach (var u in users) roleMap[u.Id] = await userManager.GetRolesAsync(u);

        var buildings = callerIsSuperAdmin
            ? await repository.GetBuildingSelectItemsAsync(cancellationToken: cancellationToken)
            : callerBuildingId != null
                ? [await repository.GetBuildingSelectItemAsync(callerBuildingId.Value, cancellationToken) ?? new SelectListItem()]
                : new List<SelectListItem>();

        return new ManageUsersPageViewModel
        {
            Filter = filter,
            Buildings = buildings,
            Total = total,
            Users = users.Select(u => new UserRowViewModel
            {
                Id = u.Id,
                Fullname = u.Fullname,
                Email = u.Email,
                PhoneNumber = u.PhoneNumber,
                BuildingId = u.BuildingId,
                BuildingName = u.Building?.Name,
                EmailConfirmed = u.EmailConfirmed,
                IsApproved = u.IsApproved,
                IsLockedOut = u.LockoutEnd != null && u.LockoutEnd > DateTimeOffset.UtcNow,
                IsPresident = roleMap[u.Id].Contains("President"),
                CreatedAt = u.CreatedAt,
                Roles = roleMap[u.Id].ToList()
            }).ToList()
        };
    }

    public async Task<(bool success, string message)> ChangeRoleAsync(string userId, string role, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        if (role is not ("Owner" or "Tenant" or "Staff")) return (false, "Invalid role.");

        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
            return (false, "Forbidden.");

        var targetIsPresident = await userManager.IsInRoleAsync(user, "President");
        if (targetIsPresident)
        {
            if (callerIsSuperAdmin)
            {
                await EnsureOnlyRolesAsync(user, role);
            }
            else
            {
                if (role == "Tenant") return (false, "A President cannot be assigned the Tenant role.");
                if (!await userManager.IsInRoleAsync(user, "President"))
                    await userManager.AddToRoleAsync(user, "President");
                if (!await userManager.IsInRoleAsync(user, "Owner"))
                    await userManager.AddToRoleAsync(user, "Owner");
            }
        }
        else
        {
            foreach (var r in new[] { "Owner", "Tenant", "Staff", "User" })
                if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
            await userManager.AddToRoleAsync(user, role);
        }

        user.IsApproved = true;
        await userManager.UpdateAsync(user);

        var displayRole = callerIsSuperAdmin && targetIsPresident ? role : (targetIsPresident ? "President + " + role : role);
        return (true, $"Changed role for {user.Fullname} to {displayRole}.");
    }

    public async Task<int> BulkChangeRoleAsync(string[] ids, string role, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        if (role is not ("Owner" or "Tenant")) return 0;

        var users = await userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        int changed = 0;

        foreach (var user in users)
        {
            if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
                continue;

            var targetIsPresident = await userManager.IsInRoleAsync(user, "President");
            if (targetIsPresident)
            {
                if (callerIsSuperAdmin)
                {
                    foreach (var r in new[] { "User", "Owner", "Tenant", "President" })
                        if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
                    await userManager.AddToRoleAsync(user, role);
                }
                else
                {
                    if (role == "Tenant") continue;
                    if (!await userManager.IsInRoleAsync(user, "President"))
                        await userManager.AddToRoleAsync(user, "President");
                    if (!await userManager.IsInRoleAsync(user, "Owner"))
                        await userManager.AddToRoleAsync(user, "Owner");
                }
            }
            else
            {
                foreach (var r in new[] { "Owner", "Tenant", "User" })
                    if (await userManager.IsInRoleAsync(user, r)) await userManager.RemoveFromRoleAsync(user, r);
                await userManager.AddToRoleAsync(user, role);
            }

            user.IsApproved = true;
            await userManager.UpdateAsync(user);
            changed++;
        }

        return changed;
    }

    // ─── Block / Unblock ──────────────────────────────────────────────────────

    public async Task<(bool success, string message)> BlockUserAsync(string userId, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
            return (false, "Forbidden.");

        var roles = await userManager.GetRolesAsync(user);
        if (roles.Contains("SuperAdmin")) return (false, "Cannot block a SuperAdmin.");
        if (roles.Contains("President") && !callerIsSuperAdmin) return (false, "Only SuperAdmin can block a President.");

        await userManager.SetLockoutEnabledAsync(user, true);
        await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
        return (true, $"Blocked {user.Fullname}.");
    }

    public async Task<int> BulkBlockAsync(string[] ids, bool callerIsSuperAdmin, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        int blocked = 0;

        foreach (var user in users)
        {
            if (!callerIsSuperAdmin && callerBuildingId != null && user.BuildingId != callerBuildingId)
                continue;

            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin")) continue;
            if (roles.Contains("President") && !callerIsSuperAdmin) continue;

            await userManager.SetLockoutEnabledAsync(user, true);
            await userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            blocked++;
        }

        return blocked;
    }

    public async Task<(bool success, string message)> UnblockUserAsync(string userId, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        if (user == null) return (false, "User not found.");

        if (callerBuildingId != null && user.BuildingId != callerBuildingId) return (false, "Forbidden.");

        await userManager.SetLockoutEndDateAsync(user, null);
        await userManager.SetLockoutEnabledAsync(user, true);
        return (true, $"Unblocked {user.Fullname}.");
    }

    public async Task<int> BulkUnblockAsync(string[] ids, Guid? callerBuildingId, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.Include(u => u.Building).Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        int unblocked = 0;

        foreach (var user in users)
        {
            if (callerBuildingId != null && user.BuildingId != callerBuildingId) continue;
            await userManager.SetLockoutEndDateAsync(user, null);
            await userManager.SetLockoutEnabledAsync(user, true);
            unblocked++;
        }

        return unblocked;
    }

    // ─── Delete ───────────────────────────────────────────────────────────────

    public async Task<(bool success, string message)> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return (false, "User not found.");

        if (await repository.HasBlockingRecordsAsync(userId, cancellationToken))
            return (false, "This user has billing history and/or active tenant assignment. For audit integrity, delete is blocked. Please deactivate the user instead.");

        var result = await userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return (false, string.Join("; ", result.Errors.Select(e => e.Description)));

        return (true, "User deleted.");
    }

    public async Task<int> BulkDeleteAsync(string[] ids, CancellationToken cancellationToken = default)
    {
        var users = await userManager.Users.Where(u => ids.Contains(u.Id)).ToListAsync(cancellationToken);
        int deleted = 0;

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            if (roles.Contains("SuperAdmin") || roles.Contains("President")) continue;

            var res = await userManager.DeleteAsync(user);
            if (res.Succeeded) deleted++;
        }

        return deleted;
    }
}
