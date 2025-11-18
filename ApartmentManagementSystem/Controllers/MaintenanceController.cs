using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class MaintenanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public MaintenanceController(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(string status = "Open")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            var q = _context.MaintenanceTickets
                .AsNoTracking()
                .Where(t => t.BuildingId == buildingId);

            if (!string.IsNullOrWhiteSpace(status))
                q = q.Where(t => t.Status == status);

            var items = await q
                .OrderBy(t => t.Status)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();

            ViewBag.SelectedStatus = status;
            return View(items);
        }

        public IActionResult Create() => View(new MaintenanceTicket());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MaintenanceTicket model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            if (!ModelState.IsValid) return View(model);

            model.Id = Guid.NewGuid();
            model.BuildingId = buildingId;
            model.Status = "Open";
            model.CreatedAt = DateTime.UtcNow;

            _context.MaintenanceTickets.Add(model);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Advance(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            var ticket = await _context.MaintenanceTickets
                .Where(t => t.Id == id && t.BuildingId == buildingId)
                .FirstOrDefaultAsync();

            if (ticket == null) return NotFound();

            ticket.Status = ticket.Status switch
            {
                "Open" => "InProgress",
                "InProgress" => "Closed",
                _ => "Closed"
            };
            if (ticket.Status == "Closed") ticket.ClosedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { status = ticket.Status });
        }
    }
}
