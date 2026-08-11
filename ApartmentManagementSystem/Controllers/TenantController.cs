using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Building;
using ApartmentManagementSystem.ViewModels.Flat;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagementSystem.Features.Tenancy.Services;

namespace ApartmentManagementSystem.Controllers
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

            var me = await _userManager.GetUserAsync(User);
            if (me == null) return Forbid();

            var flat = await _tenants.GetFlatAsync(flatId.Value);
            if (flat == null) return NotFound();

            var isOwner = flat.OwnerId == me.Id;
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            var isPresidentOfThisBuilding = User.IsInRole("President") && me.BuildingId == flat.BuildingId;

            if (!(isOwner || isSuperAdmin || isPresidentOfThisBuilding))
                return Forbid();

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["FlatId"] = flat.Id;

            var rows = await _tenants.GetFlatTenantsAsync(flat);

            return View(rows);
        }

        public async Task<IActionResult> BuildingTenants()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();
            if (User.IsInRole("President") && user.BuildingId == null) return Forbid();

            var buildingId = user.BuildingId!.Value;

            var building = await _tenants.GetBuildingAsync(buildingId);
            if (building == null) return NotFound();

            var rows = await _tenants.GetBuildingTenantsAsync(buildingId);

            ViewData["BuildingName"] = building.Name;
            ViewData["BuildingId"] = building.Id;

            return View(rows);
        }
    }
}
