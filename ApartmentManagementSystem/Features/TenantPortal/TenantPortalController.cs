using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.TenantPortal.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.TenantPortal
{
    [Authorize(Roles = Roles.Tenant)]
    public class TenantPortalController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantPortalService _tenantPortalService;

        public TenantPortalController(UserManager<ApplicationUser> userManager, ITenantPortalService tenantPortalService)
        {
            _userManager = userManager;
            _tenantPortalService = tenantPortalService;
        }

        private async Task<ApplicationUser?> GetCallerAsync() => await _userManager.GetUserAsync(User);

        public async Task<IActionResult> Dashboard()
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var vm = await _tenantPortalService.GetDashboardDataAsync(me.Id);
            
            if (vm == null)
            {
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");
            }

            return View(vm);
        }

        public async Task<IActionResult> Bills()
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var items = await _tenantPortalService.GetBillsAsync(me.Id);
            return View(items);
        }

        public async Task<IActionResult> Payments()
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var items = await _tenantPortalService.GetPaymentsAsync(me.Id);
            return View(items);
        }

        public async Task<IActionResult> Notices()
        {
            var me = await GetCallerAsync();
            if (me?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a building yet.");

            var notices = await _tenantPortalService.GetNoticesAsync(me.BuildingId);
            return View(notices);
        }

        public async Task<IActionResult> Tickets()
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(me.Id);
            
            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            var items = await _tenantPortalService.GetTicketsAsync(
                assignment.Flat.BuildingId, assignment.FlatId, me.Id);

            return View(items);
        }

        public IActionResult NewTicket() => View(new MaintenanceTicket());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewTicket(MaintenanceTicket model)
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(me.Id);

            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            if (!ModelState.IsValid) return View(model);

            model.Id = Guid.NewGuid();
            model.BuildingId = assignment.Flat.BuildingId;
            model.FlatId = assignment.FlatId;
            model.CreatedByUserId = me.Id;
            model.Status = "Open";
            model.CreatedAt = DateTime.UtcNow;

            await _tenantPortalService.CreateTicketAsync(model);

            TempData["Ok"] = "Ticket created successfully.";
            return RedirectToAction(nameof(Tickets));
        }

        public async Task<IActionResult> Visitors(DateTime? from = null, DateTime? to = null)
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(me.Id);

            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn’t linked to a flat yet.");

            var items = await _tenantPortalService.GetVisitorsAsync(
                assignment.Flat.BuildingId, assignment.FlatId, from, to);

            return View(items);
        }
    }
}
