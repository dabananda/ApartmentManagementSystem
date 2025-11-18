using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.StaffOrOwnerOrPresidentOrSuperAdmin)]
    public class EntryLogController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public EntryLogController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            IQueryable<EntryLog> entryLogsQuery = _context.EntryLogs
                .Include(x => x.Building)
                .Include(x => x.Flat);

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId.HasValue)
                {
                    entryLogsQuery = entryLogsQuery.Where(el => el.BuildingId == user.BuildingId.Value);
                }
                else
                {
                    entryLogsQuery = entryLogsQuery.Where(el => false);
                }
            }

            var entryLogs = await entryLogsQuery.ToListAsync();
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
                var flatExists = await _context.Flats
                    .AnyAsync(f => f.Id == model.FlatId && f.BuildingId == model.BuildingId);

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
                model.Id = Guid.NewGuid();
                await _context.EntryLogs.AddAsync(model);
                await _context.SaveChangesAsync();

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

            var entry = await _context.EntryLogs
                .Include(e => e.Building)
                .Include(e => e.Flat)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

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

            var entry = await _context.EntryLogs.FindAsync(id.Value);
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
                var flatExists = await _context.Flats.AnyAsync(f => f.Id == model.FlatId && f.BuildingId == model.BuildingId);
                if (!flatExists) ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
            }

            var now = DateTime.Now;
            var currentTimeWithoutSeconds = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0, 0);
            if (model.EntryTime > currentTimeWithoutSeconds) ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
            if (model.ExitTime.HasValue && model.ExitTime <= model.EntryTime) ModelState.AddModelError("ExitTime", "Exit time must be after entry time.");

            if (ModelState.IsValid)
            {
                var existing = await _context.EntryLogs.FindAsync(id);
                if (existing == null) return NotFound();

                if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
                {
                    if (user.BuildingId != existing.BuildingId) return Forbid();
                }

                existing.Fullname = model.Fullname;
                existing.PhoneNumber = model.PhoneNumber;
                existing.BuildingId = model.BuildingId;
                existing.FlatId = model.FlatId;
                existing.EntryType = model.EntryType;
                existing.NumberOfPerson = model.NumberOfPerson;
                existing.Purpose = model.Purpose;
                existing.EntryTime = model.EntryTime;
                existing.ExitTime = model.ExitTime;

                _context.Entry(existing).State = EntityState.Modified;
                await _context.SaveChangesAsync();

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

            var entry = await _context.EntryLogs
                .Include(e => e.Building)
                .Include(e => e.Flat)
                .FirstOrDefaultAsync(e => e.Id == id.Value);

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

            var entry = await _context.EntryLogs.FindAsync(id);
            if (entry == null) return NotFound();

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != entry.BuildingId) return Forbid();
            }

            _context.EntryLogs.Remove(entry);
            await _context.SaveChangesAsync();

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

            var flats = await _context.Flats
                .Where(f => f.BuildingId == buildingId)
                .Select(f => new { id = f.Id, flatNumber = f.FlatNumber })
                .OrderBy(f => f.flatNumber)
                .ToListAsync();

            return Json(flats);
        }

        private async Task PopulateDropdowns(ApplicationUser user, Guid? selectedBuildingId = null, Guid? selectedFlatId = null)
        {
            IQueryable<Building> buildingsQuery = _context.Buildings;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId.HasValue)
                {
                    buildingsQuery = buildingsQuery.Where(b => b.Id == user.BuildingId.Value);
                }
                else
                {
                    buildingsQuery = buildingsQuery.Where(b => false);
                }
            }

            var buildings = await buildingsQuery.OrderBy(b => b.Name).ToListAsync();
            ViewBag.BuildingId = new SelectList(buildings, "Id", "Name", selectedBuildingId);

            var flats = new List<Flat>();
            if (selectedBuildingId.HasValue && selectedBuildingId != Guid.Empty)
            {
                flats = await _context.Flats
                    .Where(f => f.BuildingId == selectedBuildingId.Value)
                    .OrderBy(f => f.FlatNumber)
                    .ToListAsync();
            }
            else if (user.BuildingId.HasValue)
            {
                flats = await _context.Flats
                    .Where(f => f.BuildingId == user.BuildingId.Value)
                    .OrderBy(f => f.FlatNumber)
                    .ToListAsync();
            }
            else if (await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                flats = new List<Flat>();
            }

            ViewBag.FlatId = new SelectList(flats, "Id", "FlatNumber", selectedFlatId);
        }
    }
}