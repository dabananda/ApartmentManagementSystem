using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Application.Features.Flats.Services;
using ApartmentManagementSystem.Application.Features.Flats.DTOs;
using ApartmentManagementSystem.Application.Features.President.DTOs;
using ApartmentManagementSystem.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Flats
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class FlatController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IFlatService _flats;

        public FlatController(UserManager<ApplicationUser> userManager, IFlatService flats)
        {
            _userManager = userManager;
            _flats = flats;
        }

        public async Task<IActionResult> AllFlats()
        {
            var flats = await _flats.GetAllWithReferencesAsync();
            var activeAssignments = await _flats.GetActiveAssignmentsAsync();

            var tenantMap = activeAssignments
                .GroupBy(ta => ta.FlatId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ta => ta.TenantUser.Fullname ?? ta.TenantUser.Email).ToList()
                );

            ViewData["ActiveTenantsMap"] = tenantMap;

            var ctx = await this.GetCallerContextAsync(_userManager);
            ViewData["BuildingId"] = ctx?.BuildingId;

            return View(flats);
        }

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var building = await _flats.GetBuildingAsync(buildingId.Value);
            if (building == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != buildingId) return Forbid();
            }

            var flats = await _flats.GetForBuildingAsync(buildingId.Value);

            ViewData["BuildingId"] = building.Id;
            ViewData["BuildingName"] = building.Name;

            return View(flats);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var building = await _flats.GetBuildingAsync(buildingId.Value);
            if (building == null) return NotFound();

            ViewData["BuildingId"] = buildingId;
            ViewData["BuildingName"] = building.Name;
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FlatCreateViewModel model)
        {
            if (ModelState.IsValid)
            {
                var flat = model.ToEntity();
                await _flats.CreateAsync(flat);
                return RedirectToAction(nameof(Index), new { buildingId = model.BuildingId });
            }

            var building = await _flats.GetBuildingAsync(model.BuildingId);
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }

            return View(model);
        }

        public async Task<IActionResult> AssignOwner(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var flat = await _flats.GetAsync(flatId.Value, includeReferences: true);
            if (flat == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != flat.BuildingId) return Forbid();
            }

            // Fetch owners only once
            var owners = await _userManager.GetUsersInRoleAsync(Roles.Owner);

            var viewModel = new AssignOwnerViewModel
            {
                FlatId = flat.Id,
                OwnerId = flat.OwnerId,
                Owners = new SelectList(owners, "Id", "Fullname", flat.OwnerId)
            };

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["BuildingName"] = flat.Building.Name;
            ViewData["BuildingId"] = flat.BuildingId;

            return View(viewModel);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignOwner(AssignOwnerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var flat = await _flats.GetAsync(model.FlatId);
                if (flat == null) return NotFound();

                await _flats.AssignOwnerAsync(flat, model.OwnerId);

                TempData["SuccessMessage"] = "Owner assigned successfully.";
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            var owners = await _userManager.GetUsersInRoleAsync(Roles.Owner);
            model.Owners = new SelectList(owners, "Id", "Fullname");
            model.Flats = new SelectList(await _flats.GetAllWithReferencesAsync(), "Id", "FlatNumber");

            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var flat = await _flats.GetAsync(id, includeReferences: true);
            if (flat == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != flat.BuildingId) return Forbid();
            }

            return View(flat);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, bool confirmed = false)
        {
            var flat = await _flats.GetAsync(id, asNoTracking: true);
            if (flat == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != flat.BuildingId) return Forbid();
            }

            var deletionCheck = await _flats.GetDeletionCheckAsync(id);

            if (deletionCheck.HasRelatedRecords)
            {
                var reasons = new List<string>();
                if (deletionCheck.HasBills) reasons.Add("tenant bills");
                if (deletionCheck.HasTenants) reasons.Add("tenants");
                if (deletionCheck.HasActiveAssignments) reasons.Add("active tenant assignment");
                if (deletionCheck.HasEntryLogs) reasons.Add("entry logs");

                TempData["Error"] = "Cannot delete this flat because it has " +
                                    string.Join(", ", reasons) +
                                    ". Remove/archive those records first.";
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            try
            {
                await _flats.DeleteAsync(flat);
                TempData["Success"] = "Flat deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Delete blocked by related data. Please remove dependent records first.";
            }

            return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
        }
    }
}
