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
    }
}
