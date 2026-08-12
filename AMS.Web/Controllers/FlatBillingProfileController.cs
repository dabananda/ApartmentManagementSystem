using AMS.Infrastructure.Data;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Home.DTOs;
using AMS.Application.Features.Flats.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using AMS.Application.Features.Tenancy.Commands;
using AMS.Application.Features.Tenancy.Queries;
using AMS.Application.Mediator;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class FlatBillingProfileController : Controller
    {
        private readonly UserManager<ApplicationUser> _users;
        private readonly IMediator _mediator;
        public FlatBillingProfileController(UserManager<ApplicationUser> users, IMediator mediator)
        {
            _users = users; _mediator = mediator;
        }

        public async Task<IActionResult> Index()
        {
            var me = await _users.GetUserAsync(User);

            var rows = await _mediator.Send(new GetFlatProfileRowsQuery(User.IsInRole("Owner") ? me!.Id : null));

            return View(rows);
        }

        public async Task<IActionResult> Edit(Guid flatId)
        {
            var me = await _users.GetUserAsync(User);
            var flat = await _mediator.Send(new GetAssignmentFlatQuery(flatId));
            if (flat == null) return NotFound();
            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            var p = await _mediator.Send(new GetFlatBillingProfileQuery(flatId))
                ?? new FlatBillingProfile { FlatId = flatId };

            return View(p);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(FlatBillingProfile vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var me = await _users.GetUserAsync(User);
            var flat = await _mediator.Send(new GetAssignmentFlatQuery(vm.FlatId));
            if (flat == null) return NotFound("Flat not found.");

            if (User.IsInRole("Owner") && flat.OwnerId != me!.Id) return Forbid();

            await _mediator.Send(new SaveFlatBillingProfileCommand(vm));

            TempData["Success"] = "Billing profile saved.";
            return RedirectToAction(nameof(Index));
        }
    }
}
