using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "SuperAdmin,Owner")]
    public class RentController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public RentController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(Guid? tenantId)
        {
            if (tenantId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants
                                        .Include(t => t.Flat)
                                        .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null) return NotFound();

            // Security check: Only the owner of the flat can view rent payments for the tenant
            if (tenant.Flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            // Get the list of rent payments for this tenant
            var rents = await _context.Rents
                                      .Where(r => r.TenantId == tenantId)
                                      .OrderByDescending(r => r.PaymentDate)
                                      .ToListAsync();

            ViewData["TenantName"] = tenant.Fullname;
            ViewData["TenantId"] = tenant.Id;
            return View(rents);
        }

        // GET: Rent/Create/{tenantId}
        public async Task<IActionResult> Create(Guid? tenantId)
        {
            if (tenantId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants
                                        .Include(t => t.Flat)
                                        .FirstOrDefaultAsync(t => t.Id == tenantId);

            if (tenant == null || tenant.Flat == null) return NotFound();

            // Security check before showing the form
            if (tenant.Flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin"))
            {
                return Forbid();
            }

            ViewData["TenantName"] = tenant.Fullname;
            ViewData["TenantId"] = tenant.Id;

            return View();
        }

        // POST: Rent/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("PaymentDate,Amount,Notes,TenantId")] Rent rent)
        {
            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants
                                        .Include(t => t.Flat)
                                        .FirstOrDefaultAsync(t => t.Id == rent.TenantId);

            // Security check: Verify the owner of the tenant's flat
            if (tenant == null || tenant.Flat == null || (tenant.Flat.OwnerId != user.Id && !User.IsInRole("SuperAdmin"))) // Added tenant.Flat null check
            {
                return Forbid();
            }

            if (ModelState.IsValid)
            {
                _context.Add(rent);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index), new { tenantId = rent.TenantId });
            }

            ViewData["TenantName"] = tenant.Fullname;
            ViewData["TenantId"] = rent.TenantId;
            return View(rent);
        }

        // GET: rent/details/{Guid}
        public async Task<IActionResult> Details(Guid id)
        {
            var rent = await _context.Rents.FindAsync(id);
            if (rent == null) return NotFound();
            var tenant = _context.Tenants.FirstOrDefault(t => t.Id == rent.TenantId);
            ViewData["TenantName"] = tenant.Fullname;
            return View(rent);
        }
    }
}
