using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Buildings.Services;
using ApartmentManagementSystem.Features.Buildings.ViewModels;
using ApartmentManagementSystem.Features.Shared;
using ApartmentManagementSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Features.Buildings
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class BuildingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBuildingCodeGenerator _codeGen;
        private readonly IBuildingService _buildings;

        public BuildingController(
            UserManager<ApplicationUser> userManager,
            IBuildingCodeGenerator codeGen,
            IBuildingService buildings)
        {
            _userManager = userManager;
            _codeGen = codeGen;
            _buildings = buildings;
        }

        public async Task<IActionResult> Index([FromQuery] BuildingIndexFilterViewModel filter)
        {
            return View(await _buildings.GetIndexAsync(filter));
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var building = await _buildings.GetAsync(id, includeFlats: true);
            if (building == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != building.Id) return Forbid();
            }

            return View(building);
        }

        public async Task<IActionResult> Create()
        {
            ViewData["SuggestedCode"] = await _codeGen.GenerateAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,Code")] Building building)
        {
            if (string.IsNullOrWhiteSpace(building.Code))
                building.Code = await _codeGen.GenerateAsync();

            if (await _buildings.CodeExistsAsync(building.Code))
                ModelState.AddModelError(nameof(Building.Code), "Building code already exists.");

            if (!ModelState.IsValid)
                return View(building);

            await _buildings.CreateAsync(building);
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var building = await _buildings.GetAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Address")] Building building)
        {
            if (id != building.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    await _buildings.UpdateAsync(building);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _buildings.ExistsAsync(building.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(building);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (!User.IsInRole(Roles.SuperAdmin)) return Forbid();

            var building = await _buildings.GetAsync(id);
            if (building == null) return NotFound();

            return View(building);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!User.IsInRole(Roles.SuperAdmin)) return Forbid();

            var building = await _buildings.GetAsync(id);
            if (building == null) return NotFound();

            if (await _buildings.HasBlockingRecordsAsync(id))
            {
                TempData["Error"] =
                    "Cannot delete this building because one or more flats have related records " +
                    "(bills, tenants, active assignments, or entry logs). Please remove/archive those records first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _buildings.DeleteAsync(building);
                TempData["Success"] = "Building deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Delete blocked by related data. Please remove dependent records first.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
