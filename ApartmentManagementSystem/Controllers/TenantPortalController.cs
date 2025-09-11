using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = "Tenant")]
    public class TenantPortalController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TenantPortalController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /TenantPortal/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            // Find the domain Tenant record linked to this user
            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                    .ThenInclude(f => f.Building)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant == null)
            {
                // No linked Tenant record — guide user or show empty state.
                ViewData["Message"] = "No tenant record linked to your account yet.";
                return View("DashboardEmpty");
            }

            // Pull recent rent payments and simple metrics
            var rents = await _context.Rents
                .Where(r => r.TenantId == tenant.Id)
                .OrderByDescending(r => r.PaymentDate)
                .ToListAsync();

            var lastPayment = rents.FirstOrDefault();
            var totalPaidThisYear = rents
                .Where(r => r.PaymentDate.Year == DateTime.UtcNow.Year)
                .Sum(r => r.Amount);

            var vm = new
            {
                TenantName = tenant.Fullname,
                BuildingName = tenant.Flat?.Building?.Name,
                FlatNumber = tenant.Flat?.FlatNumber,
                LastPayment = lastPayment,
                TotalPaidThisYear = totalPaidThisYear,
                TenantId = tenant.Id
            };

            return View(vm);
        }

        // GET: /TenantPortal/Payments
        public async Task<IActionResult> Payments()
        {
            var user = await _userManager.GetUserAsync(User);
            var tenant = await _context.Tenants.FirstOrDefaultAsync(t => t.UserId == user.Id);
            if (tenant == null) return Forbid();

            var rents = await _context.Rents
                .Where(r => r.TenantId == tenant.Id)
                .OrderByDescending(r => r.PaymentDate)
                .ToListAsync();

            return View(rents);
        }

        // --- TENANT NOTICES ---
        public async Task<IActionResult> Notices()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant?.Flat?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat/building yet.");

            var buildingId = tenant.Flat.BuildingId;

            // If you have IsPublished/IsActive, add: && a.IsPublished
            var notices = await _context.Announcements
                .AsNoTracking()
                .Where(a =>
                    a.BuildingId == buildingId
                    || a.BuildingId == null // treat null as global
                                            // || a.IsGlobal == true // uncomment if you have this flag instead of null BuildingId
                )
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            return View(notices);
        }

        // --- TENANT MAINTENANCE TICKETS (LIST MINE) ---
        public async Task<IActionResult> Tickets()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant?.Flat?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat/building yet.");

            var myUserId = user.Id;
            var myFlatId = tenant.FlatId;
            var buildingId = tenant.Flat.BuildingId;

            // Prefer CreatedByUserId; fall back to FlatId if CreatedByUserId column doesn't exist.
            var items = await _context.MaintenanceTickets
                .AsNoTracking()
                .Where(t =>
                    t.BuildingId == buildingId
                    && (
                        t.CreatedByUserId == myUserId
                        || (t.CreatedByUserId == null && t.FlatId == myFlatId)
                    )
                )
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            return View(items);
        }

        // --- OPEN NEW MAINTENANCE TICKET (TENANT) ---
        [HttpGet]
        public IActionResult NewTicket() => View(new MaintenanceTicket());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewTicket(MaintenanceTicket model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant?.Flat?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat/building yet.");

            if (!ModelState.IsValid) return View(model);

            model.Id = Guid.NewGuid();
            model.BuildingId = tenant.Flat.BuildingId;
            model.FlatId = tenant.FlatId;            // <-- helps fallback filter
            model.CreatedByUserId = user.Id;         // <-- key for “only my tickets”
            model.Status = "Open";
            model.CreatedAt = DateTime.UtcNow;

            _context.MaintenanceTickets.Add(model);
            await _context.SaveChangesAsync();

            TempData["Ok"] = "Ticket created successfully.";
            return RedirectToAction(nameof(Tickets));
        }

        // --- TENANT VISITORS (ENTRY LOGS FOR MY FLAT) ---
        public async Task<IActionResult> Visitors(DateTime? from = null, DateTime? to = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var tenant = await _context.Tenants
                .Include(t => t.Flat)
                .FirstOrDefaultAsync(t => t.UserId == user.Id);

            if (tenant?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            var flatId = tenant.FlatId;
            var buildingId = tenant.Flat.BuildingId;

            var q = _context.EntryLogs.AsNoTracking()
                .Where(el => el.FlatId == flatId && el.BuildingId == buildingId);

            if (from.HasValue) q = q.Where(el => el.EntryTime >= from.Value);
            if (to.HasValue) q = q.Where(el => el.EntryTime <= to.Value);

            var items = await q.OrderByDescending(el => el.EntryTime).ToListAsync();
            return View(items);
        }

        // GET: /TenantPortal/Bills
        //public async Task<IActionResult> Bills()
        //{
        //    var user = await _userManager.GetUserAsync(User);
        //    var tenant = await _context.Tenants.Include(t => t.Flat)
        //                                       .FirstOrDefaultAsync(t => t.UserId == user.Id);
        //    if (tenant?.Flat == null)
        //        return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

        //    var items = await _context.TenantBills
        //        .Where(b => b.Id == tenant.Id)
        //        .OrderByDescending(b => b.).ThenByDescending(b => b.Month)
        //        .ToListAsync();

        //    return View(items);
        //}
    }
}
