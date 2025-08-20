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
            // Get the current logged-in user
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Get the flat from the database to perform the security check
            var flat = await _context.Flats.FindAsync(tenant.FlatId);
            if (flat == null || flat.OwnerId != user.Id) return Forbid();

            if (ModelState.IsValid)
            {
                await _context.Tenants.AddAsync(tenant);
                await _context.SaveChangesAsync();

                // Redirect back to the list of tenants for the same flat
                return RedirectToAction(nameof(Index), new { flatId = tenant.FlatId });
            }

            // On validation failure, re-populate the FlatId
            ViewData["FlatId"] = tenant.FlatId;
            return View(tenant);
        }
    }
}
