using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize]
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
            var model = new EntryLog
            {
                EntryTime = DateTime.Now,
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

            // Remove validation errors for navigation properties
            ModelState.Remove("Building");
            ModelState.Remove("Flat");

            // Authorization check - ensure user can only create entries for their building
            if (!await _userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                if (user.BuildingId != model.BuildingId)
                {
                    ModelState.AddModelError("BuildingId", "You can only create entries for your assigned building.");
                }
            }

            // Validate that the flat belongs to the selected building
            if (model.BuildingId != Guid.Empty && model.FlatId != Guid.Empty)
            {
                var flatExists = await _context.Flats
                    .AnyAsync(f => f.Id == model.FlatId && f.BuildingId == model.BuildingId);

                if (!flatExists)
                {
                    ModelState.AddModelError("FlatId", "Selected flat does not belong to the selected building.");
                }
            }

            // Validate entry time
            if (model.EntryTime > DateTime.Now)
            {
                ModelState.AddModelError("EntryTime", "Entry time cannot be in the future.");
            }

            // Validate exit time
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

            // If we got this far, something failed, redisplay form
            await PopulateDropdowns(user, model.BuildingId, model.FlatId);
            return View(model);
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