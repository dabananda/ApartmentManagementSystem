using AMS.Application.Features.EntryLogs.Commands;
using AMS.Application.Features.EntryLogs.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace AMS.Web.Controllers;

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

        var isSuperAdmin = await _userManager.IsInRoleAsync(user, Roles.SuperAdmin);
        var entryLogs = await _mediator.Send(new GetEntryLogsForBuildingQuery(isSuperAdmin ? null : user.BuildingId));
        return View(entryLogs);
    }

    public async Task<IActionResult> Create()
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var model = new EntryLog
        {
            EntryTime = TrimToMinute(DateTime.Now),
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

        await NormalizeAndValidateAsync(user, model);

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
        if (!await IsWithinUserBuildingScopeAsync(user, entry.BuildingId)) return Forbid();

        return View(entry);
    }

    public async Task<IActionResult> Edit(Guid? id)
    {
        if (id == null) return NotFound();
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();

        var entry = await _mediator.Send(new GetEntryLogByIdQuery(id.Value));
        if (entry == null) return NotFound();
        if (!await IsWithinUserBuildingScopeAsync(user, entry.BuildingId)) return Forbid();

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

        await NormalizeAndValidateAsync(user, model, editErrorMessage: true);

        if (ModelState.IsValid)
        {
            var existing = await _mediator.Send(new GetEntryLogByIdQuery(id));
            if (existing == null) return NotFound();
            if (!await IsWithinUserBuildingScopeAsync(user, existing.BuildingId)) return Forbid();

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
        if (!await IsWithinUserBuildingScopeAsync(user, entry.BuildingId)) return Forbid();

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
        if (!await IsWithinUserBuildingScopeAsync(user, entry.BuildingId)) return Forbid();

        await _mediator.Send(new DeleteEntryLogCommand(entry));

        TempData["SuccessMessage"] = "Entry log deleted successfully.";
        return RedirectToAction("Index");
    }

    [HttpGet("api/flats/bybuilding/{buildingId}")]
    public async Task<IActionResult> GetFlatsByBuilding(Guid buildingId)
    {
        var user = await _userManager.GetUserAsync(User);
        if (user == null) return Unauthorized();
        if (!await IsWithinUserBuildingScopeAsync(user, buildingId)) return Forbid();

        var queryData = await _mediator.Send(new GetEntryLogFormDataQuery(buildingId));
        var flats = queryData.Flats
            .Select(f => new { id = f.Id, flatNumber = f.FlatNumber });

        return Json(flats);
    }

    /// <summary>
    /// Applies the shared create/edit validation rules: normalizes entry/exit times to
    /// whole minutes, enforces building scoping for non-super-admins, verifies the
    /// selected flat belongs to the selected building, and checks entry/exit ordering.
    /// </summary>
    private async Task NormalizeAndValidateAsync(ApplicationUser user, EntryLog model, bool editErrorMessage = false)
    {
        ModelState.Remove("Building");
        ModelState.Remove("Flat");

        model.EntryTime = model.EntryTime != default && model.EntryTime != DateTime.MinValue
            ? TrimToMinute(model.EntryTime)
            : TrimToMinute(DateTime.Now);

        if (model.ExitTime.HasValue)
        {
            model.ExitTime = TrimToMinute(model.ExitTime.Value);
        }

        ModelState.Remove("EntryTime");
        if (model.ExitTime.HasValue)
        {
            ModelState.Remove("ExitTime");
        }

        if (!await _userManager.IsInRoleAsync(user, Roles.SuperAdmin) && user.BuildingId != model.BuildingId)
        {
            var message = editErrorMessage
                ? "You can only modify entries for your assigned building."
                : "You can only create entries for your assigned building.";
            ModelState.AddModelError("BuildingId", message);
        }

        if (model.BuildingId != Guid.Empty && model.FlatId != Guid.Empty)
        {
            var flatExists = await _mediator.Send(new CheckFlatBelongsToBuildingQuery(model.FlatId, model.BuildingId));
            if (!flatExists)
            {
                ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
            }
        }

        if (model.EntryTime > TrimToMinute(DateTime.Now))
        {
            ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
        }

        if (model.ExitTime.HasValue && model.ExitTime <= model.EntryTime)
        {
            ModelState.AddModelError("ExitTime", "Exit time must be after entry time.");
        }
    }

    /// <summary>
    /// SuperAdmins may act on any building; every other role is restricted to their own
    /// assigned building. Centralizing this here keeps the scoping rule in one place
    /// instead of being re-implemented at each action.
    /// </summary>
    private async Task<bool> IsWithinUserBuildingScopeAsync(ApplicationUser user, Guid? targetBuildingId)
    {
        if (await _userManager.IsInRoleAsync(user, Roles.SuperAdmin)) return true;
        return user.BuildingId == targetBuildingId;
    }

    private static DateTime TrimToMinute(DateTime value) =>
        new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0, 0);

    private async Task PopulateDropdowns(ApplicationUser user, Guid? selectedBuildingId = null, Guid? selectedFlatId = null)
    {
        var isSuperAdmin = await _userManager.IsInRoleAsync(user, Roles.SuperAdmin);
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
