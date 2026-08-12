using AMS.Application.Features.Owner.Services;
using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Owner.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class OwnerController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOwnerService _ownerService;

        public OwnerController(UserManager<ApplicationUser> userManager, IOwnerService ownerService)
        {
            _userManager = userManager;
            _ownerService = ownerService;
        }

        private async Task<(ApplicationUser me, bool isSuperAdmin)> GetCallerInfoAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            return (me!, isSuperAdmin);
        }

        public async Task<IActionResult> Dashboard()
        {
            var (me, _) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var vm = await _ownerService.GetDashboardAsync(me.Id);
            return View(vm);
        }

        public async Task<IActionResult> OwnedFlats(string? ownerId = null)
        {
            var (me, _) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var targetOwnerId = User.IsInRole("Owner") ? me.Id : (ownerId ?? me.Id);

            var rows = await _ownerService.GetOwnedFlatsAsync(targetOwnerId);
            ViewBag.TargetOwnerId = targetOwnerId;
            return View(rows);
        }

        [HttpGet]
        public async Task<IActionResult> CommonBills(string? ownerId = null)
        {
            var (me, _) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var targetOwnerId = User.IsInRole("Owner") ? me.Id : (ownerId ?? me.Id);

            var restrictToBuildingId = (User.IsInRole("President") && me.BuildingId != null) ? me.BuildingId : null;

            var page = await _ownerService.GetCommonBillsPageAsync(targetOwnerId, restrictToBuildingId);

            // If allocations were 0, service returns an empty model with the user's name
            if (page == null)
            {
                var owner = await _userManager.FindByIdAsync(targetOwnerId);
                page = new OwnerBillsPage
                {
                    OwnerId = targetOwnerId,
                    OwnerName = owner?.Fullname ?? owner?.UserName ?? "(owner)",
                    Bills = [],
                    BuildingId = Guid.Empty,
                    History = []
                };
            }

            return View(page);
        }
    }
}
