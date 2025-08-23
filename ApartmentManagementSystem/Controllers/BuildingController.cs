using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
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

        public BuildingController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Buildings
        public async Task<IActionResult> Index()
        {
            var buildings = await _context.Buildings.ToListAsync();
            return View(buildings);
        }

        // GET: Building Details
        public async Task<IActionResult> Details(Guid id)
        {
            var building = await _context.Buildings
                .Include(b => b.Flats)
                .FirstOrDefaultAsync(b => b.Id == id);
            if (building == null)
            {
                return NotFound();
            }

            // Check for President can only see their assigned building
            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != building.Id)
                {
                    return Forbid();
                }
            }
            return View(building);
        }

        // GET: Buildings Create View
        public IActionResult Create()
        {
            return View();
        }

        // POST: Building Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,Address")] Building building)
        {
            if (ModelState.IsValid)
            {
                await _context.AddAsync(building);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(building);
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
            var building = await _context.Buildings.FindAsync(id);
            if (building == null)
            {
                return NotFound();
            }
            return View(building);
        }

        // POST: Building Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
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
    }
}