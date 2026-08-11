using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ApartmentManagementSystem.Features.Expenses.Services;

namespace ApartmentManagementSystem.Controllers
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class ExpenseAllocationController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IExpenseAllocationService _allocations;

        public ExpenseAllocationController(UserManager<ApplicationUser> userManager, IExpenseAllocationService allocations)
        {
            _userManager = userManager;
            _allocations = allocations;
        }

        public async Task<IActionResult> Index(Guid? commonBillId)
        {
            if (commonBillId == null) return NotFound();

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Forbid();

            var result = await _allocations.GetAsync(commonBillId.Value);
            var commonBill = result.CommonBill;

            if (commonBill == null || (commonBill.BuildingId != user.BuildingId && !User.IsInRole("SuperAdmin")))
            {
                return Forbid();
            }

            ViewData["CommonBillName"] = commonBill.Name;
            ViewData["BuildingId"] = commonBill.BuildingId;
            return View(result.Allocations);
        }
    }
}
