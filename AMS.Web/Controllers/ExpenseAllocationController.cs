using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Expenses.Services;
using AMS.Web.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers
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

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var result = await _allocations.GetAsync(commonBillId.Value);
            var commonBill = result.CommonBill;

            if (commonBill == null || !ctx.IsAuthorizedForBuilding(commonBill.BuildingId))
                return Forbid();

            ViewData["CommonBillName"] = commonBill.Name;
            ViewData["BuildingId"] = commonBill.BuildingId;
            return View(result.Allocations);
        }
    }
}
