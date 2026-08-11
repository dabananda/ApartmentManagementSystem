using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagementSystem.Features.Expenses.Services;

namespace ApartmentManagementSystem.Controllers
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

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var bills = await _bills.GetForBuildingAsync(buildingId.Value);

            ViewData["BuildingId"] = buildingId;
            return View(bills);
        }

        public async Task<IActionResult> Create(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != buildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = buildingId;
            return View(new CommonBill
            {
                BuildingId = buildingId.Value,
                BillDate = DateTime.Today
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Name,TotalAmount,Notes,BuildingId")] CommonBill bill)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

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

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        public async Task<IActionResult> Edit(Guid id)
        {
            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            ViewData["BuildingId"] = bill.BuildingId;
            return View(bill);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, [Bind("Id,Name,TotalAmount,Notes,BuildingId")] CommonBill input)
        {
            if (id != input.Id) return NotFound();

            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

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

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var hasPayments = await _bills.HasPaymentsAsync(bill.Id);
            ViewData["HasPayments"] = hasPayments;

            return View(bill);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(Guid id)
        {
            var bill = await _bills.GetAsync(id);
            if (bill == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (me?.BuildingId != bill.BuildingId && !User.IsInRole("SuperAdmin")) return Forbid();

            var hasPayments = await _bills.HasPaymentsAsync(bill.Id);
            if (hasPayments)
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
