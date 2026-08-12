using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Buildings.DTOs;
using AMS.Application.Features.Buildings.Queries;
using AMS.Application.Features.Buildings.Commands;
using AMS.Web.Extensions;
using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class BuildingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IBuildingCodeGenerator _codeGen;
        private readonly AMS.Application.Mediator.IMediator _mediator;

        public BuildingController(
            UserManager<ApplicationUser> userManager,
            IBuildingCodeGenerator codeGen,
            AMS.Application.Mediator.IMediator mediator)
        {
            _userManager = userManager;
            _codeGen = codeGen;
            _mediator = mediator;
        }

        public async Task<IActionResult> Index([FromQuery] BuildingIndexFilterViewModel filter)
        {
            return View(await _mediator.Send(new GetBuildingIndexQuery { Filter = filter }));
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = id, IncludeFlats = true });
            if (building == null) return NotFound();

            if (User.IsInRole(Roles.President))
            {
                var ctx = await this.GetCallerContextAsync(_userManager);
                if (ctx?.BuildingId != building.Id) return Forbid();
            }

            return View(BuildingDetailsViewModel.FromEntity(building));
        }

        public async Task<IActionResult> Create()
        {
            ViewData["SuggestedCode"] = await _codeGen.GenerateAsync();
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BuildingCreateViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Code))
                model.Code = await _codeGen.GenerateAsync();

            if (await _mediator.Send(new CheckBuildingCodeExistsQuery { Code = model.Code }))
                ModelState.AddModelError(nameof(BuildingCreateViewModel.Code), "Building code already exists.");

            if (!ModelState.IsValid)
                return View(model);

            var building = model.ToEntity();

            var command = new CreateBuildingCommand { Building = building };
            await _mediator.Send(command);

            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = id });
            if (building == null) return NotFound();
            return View(BuildingEditViewModel.FromEntity(building));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, BuildingEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var building = await _mediator.Send(new GetBuildingByIdQuery { Id = id });
                if (building == null) return NotFound();

                model.UpdateEntity(building);

                try
                {
                    await _mediator.Send(new UpdateBuildingCommand { Building = building });
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _mediator.Send(new CheckBuildingExistsQuery { Id = building.Id })) return NotFound();
                    else throw;
                }
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            if (!User.IsInRole(Roles.SuperAdmin)) return Forbid();

            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = id });
            if (building == null) return NotFound();

            return View(BuildingDetailsViewModel.FromEntity(building));
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            if (!User.IsInRole(Roles.SuperAdmin)) return Forbid();

            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = id });
            if (building == null) return NotFound();

            if (await _mediator.Send(new CheckBuildingHasBlockingRecordsQuery { BuildingId = id }))
            {
                TempData["Error"] =
                    "Cannot delete this building because one or more flats have related records " +
                    "(bills, tenants, active assignments, or entry logs). Please remove/archive those records first.";
                return RedirectToAction(nameof(Details), new { id });
            }

            try
            {
                await _mediator.Send(new DeleteBuildingCommand { Building = building });
                TempData["Success"] = "Building deleted.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Delete blocked by related data. Please remove dependent records first.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
