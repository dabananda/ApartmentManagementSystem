using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Expenses.Services;
using ApartmentManagementSystem.Features.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.Expenses
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class CommonBillController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ICommonBillService _bills;

        public CommonBillController(UserManager<ApplicationUser> userManager, ICommonBillService bills)
        {
            _userManager = userManager;
            _bills = bills;
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
        public async Task<IActionResult> Create([Bind("Name,TotalAmount,Notes,BuildingId")] CommonBill bill)
        {
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            if (ModelState.IsValid)
            {
                await _bills.CreateAsync(bill);
                return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
            }

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
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

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,TotalAmount,Notes,BuildingId")] CommonBill input)
        {
            if (id != input.Id) return NotFound();

            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();
            if (!await IsAuthorizedForBuildingAsync(bill.BuildingId)) return Forbid();

            if (!ModelState.IsValid)
            {
                ViewData["BuildingId"] = bill.BuildingId;
                return View(bill);
            }

            await _bills.UpdateAsync(bill, input);

            TempData["Success"] = "Common bill updated successfully.";
            return RedirectToAction(nameof(Index), new { buildingId = bill.BuildingId });
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
