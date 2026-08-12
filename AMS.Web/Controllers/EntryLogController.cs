using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Mediator;
using AMS.Application.Features.EntryLogs.Commands;
using AMS.Application.Features.EntryLogs.Queries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.StaffOrOwnerOrPresidentOrSuperAdmin)]
    public class EntryLogController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;

        public EntryLogController(UserManager<ApplicationUser> userManager, IMediator mediator)
        {
            _userManager = userManager;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            var entryLogs = await _mediator.Send(new GetEntryLogsForBuildingQuery(isSuperAdmin ? null : user.BuildingId));
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
                var flatExists = await _mediator.Send(new CheckFlatBelongsToBuildingQuery(model.FlatId, model.BuildingId));

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
                await _mediator.Send(new CreateEntryLogCommand(model));

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

            var entry = await _mediator.Send(new GetEntryLogByIdQuery(id.Value, IncludeReferences: true));

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

            var entry = await _mediator.Send(new GetEntryLogByIdQuery(id.Value));
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
                var flatExists = await _mediator.Send(new CheckFlatBelongsToBuildingQuery(model.FlatId, model.BuildingId));
                if (!flatExists) ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
            }

            var now = DateTime.Now;
            var currentTimeWithoutSeconds = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);
            if (model.EntryTime > currentTimeWithoutSeconds) ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
            if (model.ExitTime.HasValue && model.ExitTime <= model.EntryTime) ModelState.AddModelError("ExitTime", "Exit time must be after entry time.");

            if (ModelState.IsValid)
            {
                var existing = await _mediator.Send(new GetEntryLogByIdQuery(id));
                if (existing == null) return NotFound();

                if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                {
                    if (user.BuildingId != existing.BuildingId) return Forbid();
                }

                await _mediator.Send(new UpdateEntryLogCommand(existing, model));

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

            var entry = await _mediator.Send(new GetEntryLogByIdQuery(id.Value, IncludeReferences: true));

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

            var entry = await _mediator.Send(new GetEntryLogByIdQuery(id));
            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            await _mediator.Send(new DeleteEntryLogCommand(entry));

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

            var queryData = await _mediator.Send(new GetEntryLogFormDataQuery(buildingId));
            var flats = queryData.Flats
                .Select(f => new { id = f.Id, flatNumber = f.FlatNumber });

            return Json(flats);
        }

        private async Task PopulateDropdowns(ApplicationUser user, Guid? selectedBuildingId = null, Guid? selectedFlatId = null)
        {
            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");
            var queryData = await _mediator.Send(new GetEntryLogFormDataQuery(isSuperAdmin ? null : user.BuildingId));
            ViewBag.BuildingId = new SelectList(queryData.Buildings, "Id", "Name", selectedBuildingId);

            IReadOnlyList<Flat> flats = [];
            if (selectedBuildingId.HasValue && selectedBuildingId != Guid.Empty)
            {
                var fData = await _mediator.Send(new GetEntryLogFormDataQuery(selectedBuildingId));
                flats = fData.Flats;
            }
            else if (user.BuildingId.HasValue)
            {
                var fData = await _mediator.Send(new GetEntryLogFormDataQuery(user.BuildingId));
                flats = fData.Flats;
            }

            ViewBag.FlatId = new SelectList(flats, "Id", "FlatNumber", selectedFlatId);
        }
    }
}
