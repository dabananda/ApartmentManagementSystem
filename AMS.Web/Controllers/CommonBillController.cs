using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Expenses.Services;
using AMS.Application.Features.Expenses.DTOs;
using AMS.Web.Extensions;
using AMS.Application.Features.Buildings.Queries;
using AMS.Application.Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AMS.Web.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class CommonBillController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICommonBillService _bills;
        private readonly IMediator _mediator;

        public CommonBillController(UserManager<ApplicationUser> userManager, ICommonBillService bills, IMediator mediator)
        {
            _userManager = userManager;
            _bills = bills;
            _mediator = mediator;
        }

        /// <summary>Returns true if the current user is authorised to manage bills for <paramref name="buildingId"/>.</summary>
        private async Task<bool> IsAuthorizedForBuildingAsync(Guid buildingId)
        {
            if (User.IsInRole(Roles.SuperAdmin)) return true;
            var ctx = await this.GetCallerContextAsync(_userManager);
            return ctx?.BuildingId == buildingId;
        }

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(buildingId.Value)) return Forbid();

            var bills = await _bills.GetForBuildingAsync(buildingId.Value);

            ViewData["BuildingId"] = buildingId;
            return View(bills);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(buildingId.Value)) return Forbid();

            ViewData["BuildingId"] = buildingId;
            return View(new CommonBill
            {
                BuildingId = buildingId.Value,
                BillDate = DateTime.Today
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CommonBillCreateViewModel model)
        {
            if (!await IsAuthorizedForBuildingAsync(model.BuildingId)) return Forbid();

            if (ModelState.IsValid)
            {
                var bill = model.ToEntity();
                await _bills.CreateAsync(bill);
                return RedirectToAction(nameof(Index), new { buildingId = model.BuildingId });
            }

            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = model.BuildingId });
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }
            return View(model);
        }

        public async Task<IActionResult> Details(Guid id)
        {
            var bill = await _bills.GetAsync(id, includeBuilding: true);
            if (bill == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = bill.BuildingId });
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }

            return View(CommonBillEditViewModel.FromEntity(bill));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, CommonBillEditViewModel model)
        {
            if (id != model.Id) return NotFound();

            if (ModelState.IsValid)
            {
                var bill = await _bills.GetAsync(id);
                if (bill == null) return NotFound();
                if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

                model.UpdateEntity(bill);

                try
                {
                    await _bills.UpdateAsync(bill);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!await _bills.ExistsAsync(bill.Id)) return NotFound();
                    else throw;
                }

                TempData["Success"] = "Common bill updated successfully.";
                return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
            }

            var building = await _mediator.Send(new GetBuildingByIdQuery { Id = model.BuildingId });
            if (building != null)
            {
                ViewData["BuildingId"] = building.Id;
                ViewData["BuildingName"] = building.Name;
            }

            return View(model);
        }

        public async Task<IActionResult> Delete(Guid id)
        {
            var bill = await _bills.GetAsync(id, includeBuilding: true);
            if (bill == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            ViewData["HasPayments"] = await _bills.HasPaymentsAsync(bill.Id);
            return View(bill);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            if (await _bills.HasPaymentsAsync(bill.Id))
            {
                TempData["Error"] = "Cannot delete this common bill because there are recorded payments against it.";
                return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
            }

            await _bills.DeleteAsync(bill);

            TempData["Success"] = "Common bill deleted.";
            return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
        }
    }
}
