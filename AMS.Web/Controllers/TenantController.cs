using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Extensions;
using AMS.Application.Features.Tenancy.Queries;
using AMS.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IMediator _mediator;

        public TenantController(UserManager<ApplicationUser> userManager, IMediator mediator)
        {
            _userManager = userManager;
            _mediator = mediator;
        }

        public async Task<IActionResult> ViewTenants(Guid? flatId)
        {
            if (flatId == null) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var flat = await _mediator.Send(new GetAssignmentFlatQuery(flatId.Value));
            if (flat == null) return NotFound();

            var isOwner = flat.OwnerId == ctx.Me.Id;
            var isPresidentOfThisBuilding = User.IsInRole(Roles.President) && ctx.BuildingId == flat.BuildingId;

            if (!(isOwner || ctx.IsSuperAdmin || isPresidentOfThisBuilding))
                return Forbid();

            ViewData["FlatNumber"] = flat.FlatNumber;
            ViewData["FlatId"] = flat.Id;

            var rows = await _mediator.Send(new GetFlatTenantsQuery(flat));
            return View(rows);
        }

        public async Task<IActionResult> BuildingTenants()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();
            if (User.IsInRole(Roles.President) && ctx.BuildingId == null) return Forbid();

            var buildingId = ctx.BuildingId!.Value;

            var building = await _mediator.Send(new GetAssignmentBuildingQuery(buildingId));
            if (building == null) return NotFound();

            var rows = await _mediator.Send(new GetBuildingTenantsQuery(buildingId));

            ViewData["BuildingName"] = building.Name;
            ViewData["BuildingId"] = building.Id;

            return View(rows);
        }
    }
}
