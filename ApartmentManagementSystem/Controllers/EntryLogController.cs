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

        // GET: EntryLog
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            IQueryable<EntryLog> entryLogsQuery = _context.EntryLogs
                .Include(x => x.Building)
                .Include(x => x.Flat);

            // If user is not SuperAdmin, filter by their building
            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId.HasValue)
                {
                    entryLogsQuery = entryLogsQuery.Where(el => el.BuildingId == user.BuildingId.Value);
                }
                else
                {
                    // If user has no building assigned, show empty list
                    entryLogsQuery = entryLogsQuery.Where(el => false);
                }
            }

            var entryLogs = await entryLogsQuery.ToListAsync();
            return View(entryLogs);
        }

        // GET: EntryLog/Create
        public async Task<IActionResult> Create()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Initialize a new EntryLog with default values
            // Set time without seconds and milliseconds
            var currentTime = DateTime.Now;
            var timeWithoutSeconds = new DateTime(
                currentTime.Year,
                currentTime.Month,
                currentTime.Day,
                currentTime.Hour,
                currentTime.Minute,
                0, // seconds
                0  // milliseconds
            );

            var model = new EntryLog
            {
                EntryTime = timeWithoutSeconds,
                NumberOfPerson = 1
            };

            await PopulateDropdowns(user);
            return View(model);
        }

        // POST: EntryLog/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(EntryLog model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("Building");
            ModelState.Remove("Flat");

            // Normalize EntryTime to remove seconds and milliseconds
            if (model.EntryTime != default(DateTime) && model.EntryTime != DateTime.MinValue)
            {
                model.EntryTime = new DateTime(
                    model.EntryTime.Year,
                    model.EntryTime.Month,
                    model.EntryTime.Day,
                    model.EntryTime.Hour,
                    model.EntryTime.Minute,
                    0, // seconds
                    0  // milliseconds
                );
            }
            else
            {
                // If EntryTime is not set, use current time without seconds
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

            // Normalize ExitTime to remove seconds and milliseconds if it's set
            if (model.ExitTime.HasValue)
            {
                model.ExitTime = new DateTime(
                    model.ExitTime.Value.Year,
                    model.ExitTime.Value.Month,
                    model.ExitTime.Value.Day,
                    model.ExitTime.Value.Hour,
                    model.ExitTime.Value.Minute,
                    0, // seconds
                    0  // milliseconds
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

            // Validate entry time is not in the future (with minute precision)
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

        // GET: EntryLog/Details/5
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

        // GET: EntryLog/Edit/5
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

        // POST: EntryLog/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EntryLog model)
        {
            if (id != model.Id) return BadRequest();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            ModelState.Remove("Building");
            ModelState.Remove("Flat");

            // Normalize times
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

                // Update fields
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

        // GET: EntryLog/Delete/5
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

        // POST: EntryLog/Delete/5
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

        // API endpoint to get flats by building
        [HttpGet("api/flats/bybuilding/{buildingId}")]
        public async Task<IActionResult> GetFlatsByBuilding(Guid buildingId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Authorization check - ensure user can only access flats from their building
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
            // Populate buildings dropdown based on user role
            IQueryable<Building> buildingsQuery = _context.Buildings;

            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId.HasValue)
                {
                    buildingsQuery = buildingsQuery.Where(b => b.Id == user.BuildingId.Value);
                }
                else
                {
                    buildingsQuery = buildingsQuery.Where(b => false); // Empty list
                }
            }

            var buildings = await buildingsQuery.OrderBy(b => b.Name).ToListAsync();
            ViewBag.BuildingId = new SelectList(buildings, "Id", "Name", selectedBuildingId);

            // Populate flats dropdown
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
                // For SuperAdmin, show all flats or none initially
                flats = new List<Flat>();
            }

            ViewBag.FlatId = new SelectList(flats, "Id", "FlatNumber", selectedFlatId);
        }
    }
}