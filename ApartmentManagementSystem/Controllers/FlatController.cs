using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    public class FlatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FlatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound();

            // Authorization check for President role
            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != buildingId) return Forbid();
            }

            // Retrieve the flats for the specific building, including the owner information
            var flats = await _context.Flats
                .Include(f => f.Owner)
                .Where(f => f.BuildingId == buildingId)
                .ToListAsync();

            ViewData["BuildingId"] = building.Id;
            ViewData["BuildingName"] = building.Name;

            return View(flats);
        }

        // GET: Flat/Create
        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound();
            ViewData["BuildingId"] = buildingId;
            ViewData["BuildingName"] = building.Name;
            return View();
        }

        // POST: Flat/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlatNumber,BuildingId")] Flat flat)
        {
            if (ModelState.IsValid)
            {
                await _context.Flats.AddAsync(flat);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index", "Building");
            }
            ViewData["BuildingId"] = new SelectList(_context.Buildings, "Id", "Name", flat.BuildingId);
            return View(flat);
        }
    }
}
