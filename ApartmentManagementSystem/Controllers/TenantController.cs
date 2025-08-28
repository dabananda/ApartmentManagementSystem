using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "President, Owner,SuperAdmin")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tenant/ViewTenants/{flatId}
        public async Task<IActionResult> ViewTenants(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var flat = await _context.Flats.Include(f => f.Tenants).FirstOrDefaultAsync(f => f.Id == flatId);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["FlatId"] = flat.Id;

            return View(flat.Tenants);
        }

        // GET: Tenant/Create/{flatId}
        public async Task<IActionResult> Create(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FirstOrDefaultAsync(x => x.Id == flatId);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["FlatId"] = flatId;
            return View();
        }

        // POST: Tenant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Fullname,Email,PhoneNumber,IsActive,FlatId")] Tenant tenant)
        {
            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FirstOrDefaultAsync(f => f.Id == tenant.FlatId);

            if (flat == null || (flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                tenant.Id = Guid.NewGuid();
                _context.Add(tenant);
                // Mark the flat as occupied if it's not already
                if (!flat.IsOccupied)
                {
                    flat.IsOccupied = true;
                    _context.Update(flat);
                }
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(ViewTenants), new { flatId = tenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // GET: Tenant/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // POST: Tenant/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Fullname,Email,PhoneNumber,IsActive,FlatId")] Tenant tenant)
        {
            if (id != tenant.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var existingTenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);
            if (existingTenant == null || (existingTenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    // Update the existing tenant with the new values from the form
                    existingTenant.Fullname = tenant.Fullname;
                    existingTenant.Email = tenant.Email;
                    existingTenant.PhoneNumber = tenant.PhoneNumber;
                    existingTenant.IsActive = tenant.IsActive;

                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tenants.Any(e => e.Id == id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(ViewTenants), new { flatId = existingTenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // GET: Tenant/Details/{id}
        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }
            return View(tenant);
        }


        // GET: Tenant/Delete/{id}
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            return View(tenant);
        }

        // POST: Tenant/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null || (tenant.Flat?.OwnerId != user.Id && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            // Check if any other tenants exist for this flat. If not, mark it as vacant.
            var remainingTenants = await _context.Tenants.AnyAsync(t => t.FlatId == tenant.FlatId);
            if (!remainingTenants)
            {
                var flat = await _context.Flats.FirstOrDefaultAsync(f => f.Id == tenant.FlatId);
                if (flat != null)
                {
                    flat.IsOccupied = false;
                    _context.Update(flat);
                    await _context.SaveChangesAsync();
                }
            }

            return RedirectToAction(nameof(ViewTenants), new { flatId = tenant.FlatId });
        }

        // GET: Tenant/BuildingTenants
        public async Task<IActionResult> BuildingTenants()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            // For Presidents, get tenants from their assigned building
            // For SuperAdmin, this would need a buildingId parameter, but focusing on President use case
            if (User.IsInRole("President") && user.BuildingId == null)
            {
                return Forbid();
            }

            var buildingId = user.BuildingId.Value;

            // Get the building information
            var building = await _context.Buildings.FindAsync(buildingId);
            if (building == null) return NotFound();

            // Get all tenants in flats within the president's building
            var tenants = await _context.Tenants
                .Include(t => t.Flat)
                .ThenInclude(f => f.Owner)
                .Include(t => t.Flat)
                .ThenInclude(f => f.Building)
                .Where(t => t.Flat.BuildingId == buildingId)
                .OrderBy(t => t.Flat.FlatNumber)
                .ThenBy(t => t.Fullname)
                .ToListAsync();

            ViewData["BuildingName"] = building.Name;
            ViewData["BuildingId"] = building.Id;

            return View(tenants);
        }
    }
}