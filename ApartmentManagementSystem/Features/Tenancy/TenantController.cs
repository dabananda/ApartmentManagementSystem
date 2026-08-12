using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Shared;
using ApartmentManagementSystem.Features.Tenancy.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.Tenancy
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantDirectoryService _tenants;

        public TenantController(UserManager<ApplicationUser> userManager, ITenantDirectoryService tenants)
        {
            _userManager = userManager;
            _tenants = tenants;
        }

        public async Task<IActionResult> ViewTenants(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var flat = await _tenants.GetFlatAsync(flatId.Value);
            if (flat == null) return NotFound();

            var isOwner = flat.OwnerId == ctx.Me.Id;
            var isPresidentOfThisBuilding = User.IsInRole(Roles.President) && ctx.BuildingId == flat.BuildingId;

            if (!(isOwner || ctx.IsSuperAdmin || isPresidentOfThisBuilding))
                return Forbid();

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["FlatId"] = flat.Id;

            var rows = await _tenants.GetFlatTenantsAsync(flat);
            return View(rows);
        }

        public async Task<IActionResult> BuildingTenants()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();
            if (User.IsInRole(Roles.President) && ctx.BuildingId == null) return Forbid();

            var buildingId = ctx.BuildingId!.Value;

            var building = await _tenants.GetBuildingAsync(buildingId);
            if (building == null) return NotFound();

            var rows = await _tenants.GetBuildingTenantsAsync(buildingId);

            ViewData["BuildingName"] = building.Name;
            ViewData["BuildingId"] = building.Id;

            return View(rows);
        }
    }
}
