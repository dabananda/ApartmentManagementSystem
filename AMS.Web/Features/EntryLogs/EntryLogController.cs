using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Features.Payments;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.EntryLogs.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Web.Features.EntryLogs
{
    [Authorize(Roles = Roles.StaffOrOwnerOrPresidentOrSuperAdmin)]
    public class EntryLogController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEntryLogService _entries;

        public EntryLogController(UserManager<ApplicationUser> userManager, IEntryLogService entries)
        {
            _userManager = userManager;
            _entries = entries;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            var entryLogs = await _entries.GetForBuildingAsync(isSuperAdmin ? null : user.BuildingId);
            return View(entryLogs);
        }

        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var currentTime = DateTime.Now;
            var timeWithoutSeconds = new DateTime(
                currentTime.Year,
                currentTime.Month,
                currentTime.Day,
                currentTime.Hour,
                currentTime.Minute,
                0,
                0
            );

            var model = new EntryLog
            {
                EntryTime = timeWithoutSeconds,
                NumberOfPerson = 1
            };

            await PopulateDropdowns(user);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntryLog model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("Building");
            ModelState.Remove("Flat");

            if (model.EntryTime != default(DateTime) && model.EntryTime != DateTime.MinValue)
            {
                model.EntryTime = new DateTime(
                    model.EntryTime.Year,
                    model.EntryTime.Month,
                    model.EntryTime.Day,
                    model.EntryTime.Hour,
                    model.EntryTime.Minute,
                    0,
                    0
                );
            }
            else
            {
                var currentTime = DateTime.Now;
                model.EntryTime = new DateTime(
                    currentTime.Year,
                    currentTime.Month,
                    currentTime.Day,
                    currentTime.Hour,
                    currentTime.Minute,
                    0,
                    0
                );
            }

            if (model.ExitTime.HasValue)
            {
                model.ExitTime = new DateTime(
                    model.ExitTime.Value.Year,
                    model.ExitTime.Value.Month,
                    model.ExitTime.Value.Day,
                    model.ExitTime.Value.Hour,
                    model.ExitTime.Value.Minute,
                    0,
                    0
                );
            }

            ModelState.Remove("EntryTime");
            if (model.ExitTime.HasValue)
            {
                ModelState.Remove("ExitTime");
            }

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != model.BuildingId)
                {
                    ModelState.AddModelError("BuildingId", "You can only create entries for your assigned building.");
                }
            }

            if (model.BuildingId != Guid.Empty && model.FlatId != Guid.Empty)
            {
                var flatExists = await _entries.FlatBelongsToBuildingAsync(model.FlatId, model.BuildingId);

                if (!flatExists)
                {
                    ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
                }
            }

            var now = DateTime.Now;
            var currentTimeWithoutSeconds = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);

            if (model.EntryTime > currentTimeWithoutSeconds)
            {
                ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
            }

            if (model.ExitTime.HasValue && model.ExitTime <= model.EntryTime)
            {
                ModelState.AddModelError("ExitTime", "Exit time must be after entry time.");
            }

            if (ModelState.IsValid)
            {
                await _entries.CreateAsync(model);

                TempData["SuccessMessage"] = "Entry log created successfully.";
                return RedirectToAction("Index");
            }

            await PopulateDropdowns(user, model.BuildingId, model.FlatId);
            return View(model);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entry = await _entries.GetAsync(id.Value, includeReferences: true);

            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            return View(entry);
        }

        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entry = await _entries.GetAsync(id.Value);
            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            await PopulateDropdowns(user, entry.BuildingId, entry.FlatId);
            return View(entry);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EntryLog model)
        {
            if (id != model.Id) return BadRequest();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("Building");
            ModelState.Remove("Flat");

            if (model.EntryTime != default(DateTime) && model.EntryTime != DateTime.MinValue)
            {
                model.EntryTime = new DateTime(model.EntryTime.Year, model.EntryTime.Month, model.EntryTime.Day, model.EntryTime.Hour, model.EntryTime.Minute, 0, 0);
            }

            if (model.ExitTime.HasValue)
            {
                model.ExitTime = new DateTime(model.ExitTime.Value.Year, model.ExitTime.Value.Month, model.ExitTime.Value.Day, model.ExitTime.Value.Hour, model.ExitTime.Value.Minute, 0, 0);
            }

            ModelState.Remove("EntryTime");
            if (model.ExitTime.HasValue) ModelState.Remove("ExitTime");

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != model.BuildingId)
                {
                    ModelState.AddModelError("BuildingId", "You can only modify entries for your assigned building.");
                }
            }

            if (model.BuildingId != Guid.Empty && model.FlatId != Guid.Empty)
            {
                var flatExists = await _entries.FlatBelongsToBuildingAsync(model.FlatId, model.BuildingId);
                if (!flatExists) ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
            }

            var now = DateTime.Now;
            var currentTimeWithoutSeconds = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);
            if (model.EntryTime > currentTimeWithoutSeconds) ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
            if (model.ExitTime.HasValue && model.ExitTime <= model.EntryTime) ModelState.AddModelError("ExitTime", "Exit time must be after entry time.");

            if (ModelState.IsValid)
            {
                var existing = await _entries.GetAsync(id);
                if (existing == null) return NotFound();

                if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                {
                    if (user.BuildingId != existing.BuildingId) return Forbid();
                }

                await _entries.UpdateAsync(existing, model);

                TempData["SuccessMessage"] = "Entry log updated successfully.";
                return RedirectToAction("Index");
            }

            await PopulateDropdowns(user, model.BuildingId, model.FlatId);
            return View(model);
        }

        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entry = await _entries.GetAsync(id.Value, includeReferences: true);

            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            return View(entry);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var entry = await _entries.GetAsync(id);
            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            await _entries.DeleteAsync(entry);

            TempData["SuccessMessage"] = "Entry log deleted successfully.";
            return RedirectToAction("Index");
        }

        [HttpGet("api/flats/bybuilding/{buildingId}")]
        public async Task<IActionResult> GetFlatsByBuilding(Guid buildingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != buildingId)
                {
                    return Forbid();
                }
            }

            var flats = (await _entries.GetFlatsAsync(buildingId))
                .Select(f => new { id = f.Id, flatNumber = f.FlatNumber });

            return Json(flats);
        }

        private async Task PopulateDropdowns(ApplicationUser user, Guid? selectedBuildingId = null, Guid? selectedFlatId = null)
        {
            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            var buildings = await _entries.GetBuildingsAsync(isSuperAdmin ? null : user.BuildingId);
            ViewBag.BuildingId = new SelectList(buildings, "Id", "Name", selectedBuildingId);

            IReadOnlyList<Flat> flats = [];
            if (selectedBuildingId.HasValue && selectedBuildingId != Guid.Empty)
            {
                flats = await _entries.GetFlatsAsync(selectedBuildingId);
            }
            else if (user.BuildingId.HasValue)
            {
                flats = await _entries.GetFlatsAsync(user.BuildingId);
            }

            ViewBag.FlatId = new SelectList(flats, "Id", "FlatNumber", selectedFlatId);
        }
    }
}
