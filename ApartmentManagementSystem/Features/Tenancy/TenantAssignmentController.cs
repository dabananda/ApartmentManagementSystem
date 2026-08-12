using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Shared;
using ApartmentManagementSystem.Application.Features.Tenancy.Services;
using ApartmentManagementSystem.Application.Features.Tenancy.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Tenancy
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantAssignmentController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly ITenantAssignmentService _assignments;

        public TenantAssignmentController(UserManager<ApplicationUser> users, ITenantAssignmentService assignments)
        {
            _users = users;
            _assignments = assignments;
        }

        public async Task<IActionResult> Assign()
        {
            var ctx = await this.GetCallerContextAsync(_users);
            var vm = await _assignments.GetAssignmentFormAsync(User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null);
            return View(vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignTenantVM vm)
        {
            if (!ModelState.IsValid) return await Assign();

            var ctx = await this.GetCallerContextAsync(_users);
            var flat = await _assignments.GetFlatAsync(vm.FlatId);
            if (flat == null) return NotFound("Flat not found.");

            if (User.IsInRole(Roles.Owner) && flat.OwnerId != ctx?.Me.Id) return Forbid();

            if (!await _assignments.TenantExistsAsync(vm.TenantUserId)) return NotFound("Tenant user not found.");

            var activeForTenant = await _assignments.GetActiveAssignmentAsync(vm.TenantUserId);
            if (activeForTenant != null)
            {
                ModelState.AddModelError(string.Empty, activeForTenant.FlatId == vm.FlatId
                    ? "This tenant is already assigned to this flat."
                    : "This tenant is already assigned to another flat.");
                return await Assign();
            }

            try
            {
                await _assignments.AssignAsync(vm.FlatId, vm.TenantUserId);
            }
            catch (DbUpdateException ex) when (
                ex.InnerException?.Message.Contains("IX_TenantAssignments_TenantUserId_Active") == true ||
                ex.InnerException?.Message.Contains("IX_TenantAssignments_FlatId_Active") == true)
            {
                ModelState.AddModelError(string.Empty, "Another assignment already exists. Please refresh and try again.");
                return await Assign();
            }

            TempData["Success"] = "Tenant assigned to flat.";
            return RedirectToAction(nameof(MyTenants));
        }

        public async Task<IActionResult> MyTenants()
        {
            var ctx = await this.GetCallerContextAsync(_users);
            var data = await _assignments.GetActiveAsync(User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null);
            return View(data);
        }
    }
}
