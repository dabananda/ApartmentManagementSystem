using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin, President")]
    public class BuildingController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBuildingCodeGenerator _codeGen;

        public BuildingController(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            IBuildingCodeGenerator codeGen)
        {
            _context = context;
            _userManager = userManager;
            _codeGen = codeGen;
        }

        // GET: Buildings
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var isSuperAdmin = await _userManager.IsInRoleAsync(user, "SuperAdmin");

            IQueryable<Building> buildingsQuery = _context.Buildings;

            // If the user is a President, filter the buildings by their assigned BuildingId
            if (!isSuperAdmin)
            {
                buildingsQuery = buildingsQuery.Where(b => b.Id == user.BuildingId);
            }

            var buildings = await buildingsQuery.ToListAsync();
            return View(buildings);
        }

        // GET: Building Details
        public async Task<IActionResult> Details(Guid id)
        {
            var building = await _context.Buildings
                .Include(b => b.Flats)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (building == null) return NotFound();

            // Check for President can only see their assigned building
            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != building.Id) return Forbid();
            }
            return View(building);
        }

        // GET: Buildings Create View
        public async Task<IActionResult> Create()
        {
            ViewData["SuggestedCode"] = await _codeGen.GenerateAsync();
            return View();
        }

        // POST: Building Create
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address,Code")] Building building)
        {
            if (string.IsNullOrWhiteSpace(building.Code))
                building.Code = await _codeGen.GenerateAsync();

            if (await _context.Buildings.AnyAsync(b => b.Code == building.Code))
                ModelState.AddModelError(nameof(Building.Code), "Building code already exists.");

            if (!ModelState.IsValid)
                return View(building);

            await _context.AddAsync(building);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // GET: Buildings Edit View
        public async Task<IActionResult> Edit(Guid id)
        {
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: Building Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,Address")] Building building)
        {
            if (id != building.Id) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(building);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!BuildingExists(building.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }
            return View(building);
        }

        // GET: Buildings Delete View
        public async Task<IActionResult> Delete(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!User.IsInRole("SuperAdmin")) return Forbid();
            var building = await _context.Buildings.FindAsync(id);
            if (building == null) return NotFound();
            return View(building);
        }

        // POST: Building Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            if (!User.IsInRole("SuperAdmin")) return Forbid();
            var building = await _context.Buildings.FindAsync(id);
            if (building != null)
            {
                _context.Buildings.Remove(building);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // Helper method to check if building exists
        private bool BuildingExists(Guid id)
        {
            return _context.Buildings.Any(e => e.Id == id);
        }

        // GET: Building/MyBuildings
        [Authorize(Roles = "President")]
        public async Task<IActionResult> MyBuildings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var buildings = await _context.Buildings
                                          .Where(b => b.Id == user.BuildingId)
                                          .ToListAsync();

            return View("Index", buildings);
        }
    }
}