using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Shared;
using ApartmentManagementSystem.Application.Features.TenantPortal.Services;
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

        public async Task<IActionResult> Dashboard()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var vm = await _tenantPortalService.GetDashboardDataAsync(ctx.Me.Id);
            if (vm == null)
                return View("TenantSetupRequired", "Your account isn't linked to a flat yet.");

            return View(vm);
        }

        public async Task<IActionResult> Bills()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var items = await _tenantPortalService.GetBillsAsync(ctx.Me.Id);
            return View(items);
        }

        public async Task<IActionResult> Payments()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var items = await _tenantPortalService.GetPaymentsAsync(ctx.Me.Id);
            return View(items);
        }

        public async Task<IActionResult> Notices()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx?.BuildingId == null)
                return View("TenantSetupRequired", "Your account isn't linked to a building yet.");

            var notices = await _tenantPortalService.GetNoticesAsync(ctx.BuildingId);
            return View(notices);
        }

        public async Task<IActionResult> Tickets()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(ctx.Me.Id);
            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn't linked to a flat yet.");

            var items = await _tenantPortalService.GetTicketsAsync(
                assignment.Flat.BuildingId, assignment.FlatId, ctx.Me.Id);

            return View(items);
        }

        public IActionResult NewTicket() => View(new MaintenanceTicket());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> NewTicket(MaintenanceTicket model)
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(ctx.Me.Id);
            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn't linked to a flat yet.");

            if (!ModelState.IsValid) return View(model);

            model.BuildingId = assignment.Flat.BuildingId;
            model.FlatId = assignment.FlatId;
            model.CreatedByUserId = ctx.Me.Id;
            model.Status = "Open";
            model.CreatedAt = DateTime.UtcNow;

            await _tenantPortalService.CreateTicketAsync(model);

            TempData["Ok"] = "Ticket created successfully.";
            return RedirectToAction(nameof(Tickets));
        }

        public async Task<IActionResult> Visitors(DateTime? from = null, DateTime? to = null)
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var (assignment, _) = await _tenantPortalService.GetActiveAssignmentAsync(ctx.Me.Id);
            if (assignment?.Flat == null)
                return View("TenantSetupRequired", "Your account isn't linked to a flat yet.");

            var items = await _tenantPortalService.GetVisitorsAsync(
                assignment.Flat.BuildingId, assignment.FlatId, from, to);

            return View(items);
        }
    }
}
