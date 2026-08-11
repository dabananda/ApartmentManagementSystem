using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagementSystem.Features.Maintenance.Services;

namespace ApartmentManagementSystem.Features.Maintenance
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class MaintenanceController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMaintenanceService _maintenance;

        public MaintenanceController(UserManager<ApplicationUser> userManager, IMaintenanceService maintenance)
        {
            _userManager = userManager;
            _maintenance = maintenance;
        }

        public async Task<IActionResult> Index(string status = "Open")
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            var items = await _maintenance.GetForBuildingAsync(buildingId, status);

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

            await _maintenance.CreateAsync(model, buildingId);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Advance(Guid id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId == null) return Forbid();
            var buildingId = user.BuildingId.Value;

            var ticket = await _maintenance.AdvanceAsync(id, buildingId);

            if (ticket == null) return NotFound();

            return RedirectToAction(nameof(Index), new { status = ticket.Status });
        }
    }
}
