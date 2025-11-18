using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.President;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class FlatController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public FlatController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> AllFlats()
        {
            var flats = await _context.Flats
                            .Include(f => f.Owner)
                            .Include(f => f.Building)
                            .ToListAsync();

            var activeAssignments = await _context.TenantAssignments
                .Include(ta => ta.TenantUser)
                .Where(ta => ta.EndDate == null)
                .ToListAsync();

            var tenantMap = activeAssignments
                .GroupBy(ta => ta.FlatId)
                .ToDictionary(
                    g => g.Key,
                    g => g.Select(ta => ta.TenantUser.Fullname ?? ta.TenantUser.Email).ToList()
                );

            ViewData["ActiveTenantsMap"] = tenantMap;

            var currentUser = await _userManager.GetUserAsync(User);
            ViewData["BuildingId"] = currentUser?.BuildingId;

            return View(flats);
        }

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
                            .Include(f => f.Tenants)
                            .Where(f => f.BuildingId == buildingId)
                            .ToListAsync();

            ViewData["BuildingId"] = building.Id;
            ViewData["BuildingName"] = building.Name;

            return View(flats);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound();
            ViewData["BuildingId"] = buildingId;
            ViewData["BuildingName"] = building.Name;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("FlatNumber,BuildingId")] Flat flat)
        {
            if (ModelState.IsValid)
            {
                await _context.Flats.AddAsync(flat);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            var building = await _context.Buildings.FindAsync(flat.BuildingId);
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }

            return View(flat);
        }

        public async Task<IActionResult> AssignOwner(Guid? flatId)
        {
            if (flatId == null) return NotFound();
            var flat = await _context.Flats
                .Include(f => f.Building)
                .FirstOrDefaultAsync(f => f.Id == flatId);
            if (flat == null) return NotFound();

            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != flat.BuildingId) return Forbid();
            }

            var ownersInRole = await _userManager.GetUsersInRoleAsync("Owner");

            var owners = await _userManager.GetUsersInRoleAsync("Owner");

            var viewModel = new AssignOwnerViewModel
            {
                FlatId = flat.Id,
                OwnerId = flat.OwnerId,
                Owners = new SelectList(owners, "Id", "Fullname", flat.OwnerId)
            };

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["BuildingName"] = flat.Building.Name;
            ViewData["BuildingId"] = flat.BuildingId;

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignOwner(AssignOwnerViewModel model)
        {
            if (ModelState.IsValid)
            {
                var flat = await _context.Flats.FindAsync(model.FlatId);
                if (flat == null) return NotFound();

                flat.OwnerId = model.OwnerId;
                flat.IsOccupied = !string.IsNullOrEmpty(model.OwnerId);

                _context.Update(flat);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Owner assigned successfully.";
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            var owners = await _userManager.GetUsersInRoleAsync("Owner");
            model.Owners = new SelectList(owners, "Id", "Fullname");
            model.Flats = new SelectList(_context.Flats.ToList(), "Id", "FlatNumber");

            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var flat = await _context.Flats
                .Include(f => f.Building)
                .Include(f => f.Owner)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flat == null) return NotFound();

            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user.BuildingId != flat.BuildingId) return Forbid();
            }

            return View(flat);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id, bool confirmed = false)
        {
            var flat = await _context.Flats
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);

            if (flat == null) return NotFound();

            if (User.IsInRole("President"))
            {
                var user = await _userManager.GetUserAsync(User);
                if (user?.BuildingId != flat.BuildingId) return Forbid();
            }

            var hasBills = await _context.TenantBills.AnyAsync(b => b.FlatId == id);
            var hasTenants = await _context.Tenants.AnyAsync(t => t.FlatId == id);
            var hasActiveAssign = await _context.TenantAssignments.AnyAsync(a => a.FlatId == id && a.EndDate == null);
            var hasEntryLogs = await _context.EntryLogs.AnyAsync(e => e.FlatId == id);

            if (hasBills || hasTenants || hasActiveAssign || hasEntryLogs)
            {
                var reasons = new List<string>();
                if (hasBills) reasons.Add("tenant bills");
                if (hasTenants) reasons.Add("tenants");
                if (hasActiveAssign) reasons.Add("active tenant assignment");
                if (hasEntryLogs) reasons.Add("entry logs");

                TempData["Error"] = "Cannot delete this flat because it has " +
                                    string.Join(", ", reasons) +
                                    ". Remove/archive those records first.";
                return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
            }

            try
            {
                _context.Flats.Remove(new Flat { Id = id });
                await _context.SaveChangesAsync();
                TempData["Success"] = "Flat deleted successfully.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Delete blocked by related data. Please remove dependent records first.";
            }

            return RedirectToAction(nameof(Index), new { buildingId = flat.BuildingId });
        }
    }
}
