using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels;
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

                // Redirects to the Flat/Index for the specific building
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            // On validation failure, re-populate the ViewData with the correct values
            var building = await _context.Buildings.FindAsync(flat.BuildingId);
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }

            return View(flat);
        }

        // GET: Flat/AssignOwner/{flatId}
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> AssignOwner(Guid? flatId)
        {
            if (flatId == null) return NotFound();
            var flat = await _context.Flats.Include(f => f.Building).FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            // President authorization: can only assign flats in their building
            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != flat.BuildingId) return Forbid();
            }

            // Get a list of users with the "Owner" role
            var owners = await _userManager.GetUsersInRoleAsync("Owner");

            var viewModel = new AssignOwnerViewModel
            {
                FlatId = flat.Id,
                Owners = new SelectList(owners, "Id", "Fullname")
            };

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["BuildingName"] = flat.Building.Name;

            return View(viewModel);
        }

        // POST: Flat/AssignOwner
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "SuperAdmin,President")]
        public async Task<IActionResult> AssignOwner(AssignOwnerViewModel viewModel)
        {
            if (ModelState.IsValid)
            {
                var flat = await _context.Flats.FindAsync(viewModel.FlatId);
                if (flat == null)
                {
                    return NotFound();
                }

                // Assign the owner to the flat
                flat.OwnerId = viewModel.OwnerId;
                await _context.SaveChangesAsync();

                // Redirect back to the flats list for the same building
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            // If the model state is not valid, re-populate the ViewModel and return the view
            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            viewModel.Owners = new SelectList(owners, "Id", "Fullname");
            return View(viewModel);
        }

        // GET: Flat/MyFlats
        [Authorize(Roles = "Owner")]
        public async Task<IActionResult> MyFlats()
        {
            // Get the current user
            var user = await _userManager.GetUserAsync(User);

            // Check if the user is null, which shouldn't happen with [Authorize] but is a good practice
            if (user == null)
            {
                return Forbid();
            }

            // Retrieve only the flats owned by the current user
            var myFlats = await _context.Flats
                .Include(f => f.Building)
                .Where(f => f.OwnerId == user.Id)
                .ToListAsync();

            return View("Index", myFlats);
        }
    }
}
