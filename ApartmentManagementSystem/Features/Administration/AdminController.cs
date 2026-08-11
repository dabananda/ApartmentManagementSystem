using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.Administration.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Features.Administration.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Administration
{
    public class AdminController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IUserManagementService _userManagement;

        public AdminController(UserManager<ApplicationUser> userManager, IUserManagementService userManagement)
        {
            _userManager = userManager;
            _userManagement = userManagement;
        }

        // ─── Helpers ─────────────────────────────────────────────────────────

        private async Task<(ApplicationUser me, bool isSuperAdmin, Guid? buildingId)> GetCallerInfoAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            return (me!, isSuperAdmin, me?.BuildingId);
        }

        // ─── President assignment ─────────────────────────────────────────────

        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> AssignPresident(Guid? buildingId)
        {
            var buildings = await _userManagement.GetBuildingSelectItemsAsync();
            var owners = buildingId.HasValue
                ? await _userManagement.GetOwnersForBuildingSelectAsync(buildingId.Value)
                : new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>();

            var vm = new AssignPresidentViewModel
            {
                BuildingId = buildingId,
                Buildings = buildings,
                Owners = owners
            };

            return View(vm);
        }


        [Authorize(Roles = Roles.SuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignPresident(AssignPresidentViewModel model)
        {
            if (!ModelState.IsValid || !model.BuildingId.HasValue)
            {
                ModelState.AddModelError(nameof(model.BuildingId), "Please select a building.");
                model.Buildings = await _userManagement.GetBuildingSelectItemsAsync();
                model.Owners = model.BuildingId.HasValue
                    ? await _userManagement.GetOwnersForBuildingSelectAsync(model.BuildingId.Value)
                    : [];
                return View(model);
            }

            var me = await _userManager.GetUserAsync(User);
            var (success, message) = await _userManagement.AssignPresidentAsync(
                model.BuildingId!.Value, model.OwnerUserId!, me!.Id);

            if (!success)
            {
                ModelState.AddModelError(nameof(model.OwnerUserId), message);
                model.Buildings = await _userManagement.GetBuildingSelectItemsAsync();
                model.Owners = await _userManagement.GetOwnersForBuildingSelectAsync(model.BuildingId.Value);
                return View(model);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(AssignPresident), new { buildingId = model.BuildingId });
        }


        [Authorize(Roles = Roles.SuperAdmin)]
        public async Task<IActionResult> OwnersForBuilding(Guid buildingId)
        {
            var owners = (await _userManagement.GetOwnersForBuildingSelectAsync(buildingId))
                .Select(o => new { value = o.Value, text = o.Text });
            return Json(owners);
        }

        // ─── Create / Edit user ───────────────────────────────────────────────

        [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
        public async Task<IActionResult> CreateUser()
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            var buildingItems = isSuperAdmin
                ? await _userManagement.GetBuildingSelectItemsAsync()
                : me?.BuildingId != null
                    ? await _userManagement.GetBuildingSelectItemsAsync(me.BuildingId)
                    : [];

            ViewBag.Buildings = buildingItems;
            ViewBag.Roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new("User", "User"), new("Staff", "Staff"), new("Tenant", "Tenant"), new("Owner", "Owner")
            };

            var vm = new CreateUserViewModel();
            if (User.IsInRole("President") && me?.BuildingId != null) vm.BuildingId = me.BuildingId.Value;
            return View(vm);
        }

        [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateUser(CreateUserViewModel model)
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            async Task LoadListsAsync()
            {
                ViewBag.Buildings = isSuperAdmin
                    ? await _userManagement.GetBuildingSelectItemsAsync()
                    : me?.BuildingId != null
                        ? await _userManagement.GetBuildingSelectItemsAsync(me.BuildingId)
                        : [];
                ViewBag.Roles = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
                {
                    new("User", "User"), new("Staff", "Staff"), new("Tenant", "Tenant"), new("Owner", "Owner")
                };
            }

            if (!ModelState.IsValid)
            {
                await LoadListsAsync();
                return View(model);
            }

            if (User.IsInRole("President") && (me?.BuildingId == null || me.BuildingId != model.BuildingId))
            {
                ModelState.AddModelError(nameof(model.BuildingId), "You can only create users in your building.");
                await LoadListsAsync();
                return View(model);
            }

            if (await _userManager.FindByEmailAsync(model.Email) != null)
            {
                ModelState.AddModelError(nameof(model.Email), "A user with this email already exists.");
                await LoadListsAsync();
                return View(model);
            }

            var (success, errors) = await _userManagement.CreateUserAsync(model, me!.Id);
            if (!success)
            {
                foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
                await LoadListsAsync();
                return View(model);
            }

            TempData["Success"] = $"Created user {model.Fullname} ({model.Email}).";
            return RedirectToAction(nameof(Users), new { BuildingId = model.BuildingId });
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        public async Task<IActionResult> EditUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var me = await _userManager.GetUserAsync(User);
            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null || user.BuildingId != me.BuildingId) return Forbid();
            }

            var isSuperAdmin = User.IsInRole("SuperAdmin");
            ViewBag.Buildings = isSuperAdmin
                ? await _userManagement.GetBuildingSelectItemsAsync()
                : user.BuildingId != null
                    ? await _userManagement.GetBuildingSelectItemsAsync(user.BuildingId)
                    : [];

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                Fullname = user.Fullname,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                BuildingId = user.BuildingId,
                BuildingName = user.Building?.Name,
                IsSuperAdminCaller = isSuperAdmin
            };

            return View(vm);
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EditUser(EditUserViewModel model)
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");

            if (!ModelState.IsValid)
            {
                ViewBag.Buildings = isSuperAdmin
                    ? await _userManagement.GetBuildingSelectItemsAsync()
                    : model.BuildingId != null
                        ? await _userManagement.GetBuildingSelectItemsAsync(model.BuildingId)
                        : [];
                return View(model);
            }

            var user = await _userManager.Users.Include(u => u.Building).FirstOrDefaultAsync(u => u.Id == model.Id);
            if (user == null) return NotFound();

            if (User.IsInRole("President"))
            {
                if (me?.BuildingId == null || user.BuildingId != me.BuildingId) return Forbid();
            }

            var (success, errors) = await _userManagement.UpdateUserAsync(model, isSuperAdmin);
            if (!success)
            {
                foreach (var e in errors) ModelState.AddModelError(string.Empty, e);
                ViewBag.Buildings = isSuperAdmin
                    ? await _userManagement.GetBuildingSelectItemsAsync()
                    : user.BuildingId != null
                        ? await _userManagement.GetBuildingSelectItemsAsync(user.BuildingId)
                        : [];
                return View(model);
            }

            TempData["Success"] = $"Updated user {model.Fullname}.";
            return RedirectToAction(nameof(Users), new { BuildingId = model.BuildingId });
        }

        // ─── Approvals ────────────────────────────────────────────────────────

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        public async Task<IActionResult> Approvals([FromQuery] ApprovalsFilterViewModel filter)
        {
            var (me, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            if (User.IsInRole("President") && buildingId == null) return Forbid();

            var vm = await _userManagement.GetApprovalsPageAsync(filter, buildingId, isSuperAdmin);
            return View(vm);
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveUser(string id, string role)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (me, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var (success, message) = await _userManagement.ApproveUserAsync(id, role, me!.Id, isSuperAdmin, buildingId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Approvals));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkApprove(string[] ids, string role)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Approvals));
            }

            var (me, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var applied = await _userManagement.BulkApproveAsync(ids, role, me!.Id, isSuperAdmin, buildingId);

            TempData["Success"] = $"Applied {applied} update(s).";
            return RedirectToAction(nameof(Approvals));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var (success, message) = await _userManagement.ResetUserAsync(id, isSuperAdmin, buildingId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Approvals));
        }

        // ─── Manage users ─────────────────────────────────────────────────────

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpGet]
        public async Task<IActionResult> Users([FromQuery] ManageUsersFilterViewModel filter)
        {
            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            if (User.IsInRole("President") && buildingId == null) return Forbid();

            var vm = await _userManagement.GetUsersPageAsync(filter, buildingId, isSuperAdmin);
            return View(vm);
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeRole(string id, string role)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var (success, message) = await _userManagement.ChangeRoleAsync(id, role, isSuperAdmin, buildingId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkChangeRole(string[] ids, string role)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var changed = await _userManagement.BulkChangeRoleAsync(ids, role, isSuperAdmin, buildingId);

            TempData["Success"] = $"Changed roles for {changed} user(s).";
            return RedirectToAction(nameof(Users));
        }

        // ─── Block / Unblock ──────────────────────────────────────────────────

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BlockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var (success, message) = await _userManagement.BlockUserAsync(id, isSuperAdmin, buildingId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkBlock(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var (_, isSuperAdmin, buildingId) = await GetCallerInfoAsync();
            var blocked = await _userManagement.BulkBlockAsync(ids, isSuperAdmin, buildingId);

            TempData["Success"] = $"Blocked {blocked} user(s).";
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> UnblockUser(string id)
        {
            if (string.IsNullOrWhiteSpace(id)) return BadRequest();

            var (_, _, buildingId) = await GetCallerInfoAsync();

            // Presidents can only unblock within their building
            var callerBuildingId = User.IsInRole("SuperAdmin") ? null : buildingId;
            var (success, message) = await _userManagement.UnblockUserAsync(id, callerBuildingId);

            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkUnblock(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var (_, _, buildingId) = await GetCallerInfoAsync();
            var callerBuildingId = User.IsInRole("SuperAdmin") ? null : buildingId;
            var unblocked = await _userManagement.BulkUnblockAsync(ids, callerBuildingId);

            TempData["Success"] = $"Unblocked {unblocked} user(s).";
            return RedirectToAction(nameof(Users));
        }

        // ─── Delete ───────────────────────────────────────────────────────────

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteUser(string id)
        {
            var (success, message) = await _userManagement.DeleteUserAsync(id);
            TempData[success ? "Success" : "Error"] = message;
            return RedirectToAction(nameof(Users));
        }

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> BulkDelete(string[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                TempData["Error"] = "No users selected.";
                return RedirectToAction(nameof(Users));
            }

            var deleted = await _userManagement.BulkDeleteAsync(ids);
            TempData["Success"] = $"Deleted {deleted} user(s).";
            return RedirectToAction(nameof(Users));
        }

        // ─── Legacy actions (kept for backward compat, delegate to Users) ─────

        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        public async Task<IActionResult> ApproveOwners()
        {
            var users = await _userManager.GetUsersInRoleAsync("User");
            return View(users);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
        public async Task<IActionResult> ApproveOwner(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null) return NotFound();

            var result = await _userManager.AddToRoleAsync(user, "Owner");
            if (!result.Succeeded) TempData["Error"] = "Failed to approve owner.";

            return RedirectToAction(nameof(ApproveOwners));
        }
    }
}
