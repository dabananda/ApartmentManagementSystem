using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "Owner")]
    public class TenantController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Tenant
        public async Task<IActionResult> Index(Guid? flatId)
        {
            if (flatId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();
            var flat = await _context.Flats.Include(f => f.Tenants).FirstOrDefaultAsync(x => x.Id == flatId);
            if (flat == null) return NotFound();
            if (flat.OwnerId != user.Id) return Forbid();
            ViewData["FlatId"] = flatId;
            ViewData["FlatNumber"] = flat.FlatNumber;
            return View(flat.Tenants);
        }

        // GET: Tenant/Create/{flatId}
        public async Task<IActionResult> Create(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            // Get the current logged-in user and the flat
            var user = await _userManager.GetUserAsync(User);
            var flat = await _context.Flats.FindAsync(flatId);

            if (flat == null) return NotFound();

            // Security check: an Owner can only create tenants for their flats
            if (flat.OwnerId != user.Id) return Forbid();

            // Pass the FlatId to the view for the form submission
            ViewData["FlatId"] = flat.Id;
            return View();
        }

        // POST: Tenant/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Fullname,Email,PhoneNumber,FlatId")] Tenant tenant)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var flat = await _context.Flats.FindAsync(tenant.FlatId);
            if (flat == null || flat.OwnerId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Index), new { flatId = tenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        public async Task<IActionResult> Details(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .Include(t => t.Rents)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null) return NotFound();

            if (tenant.Flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            return View(tenant);
        }

        // GET: Tenant/Edit/{id}
        public async Task<IActionResult> Edit(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();
            if (tenant.Flat.OwnerId != user.Id) return Forbid();

            return View(tenant);
        }

        // POST: Tenant/Edit/{id}
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Fullname,Email,PhoneNumber,IsActive,FlatId")] Tenant tenant)
        {
            if (id != tenant.Id) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var existingFlat = await _context.Flats.FindAsync(tenant.FlatId);

            if (existingFlat.OwnerId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(tenant);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Tenants.Any(e => e.Id == tenant.Id)) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index), new { flatId = tenant.FlatId });
            }

            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }

        // GET: Tenant/Delete/{id}
        public async Task<IActionResult> Delete(Guid? id)
        {
            if (id == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (tenant == null) return NotFound();
            if (tenant.Flat.OwnerId != user.Id) return Forbid();

            return View(tenant);
        }

        // POST: Tenant/Delete/{id}
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.Include(t => t.Flat).FirstOrDefaultAsync(t => t.Id == id);

            if (tenant == null) return NotFound();

            if (tenant.Flat.OwnerId != user.Id) return Forbid();

            _context.Tenants.Remove(tenant);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { flatId = tenant.FlatId });
        }
    }
}
