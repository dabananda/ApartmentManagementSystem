using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.Owner.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Infrastructure.Services;
using ApartmentManagementSystem.Features.Owner.ViewModels;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;

namespace ApartmentManagementSystem.Features.Owner
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class OwnerBillingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOwnerBillingService _ownerBillingService;
        private readonly IPaymentEmailService _paymentEmailService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;

        public OwnerBillingController(
            UserManager<ApplicationUser> userManager,
            IOwnerBillingService ownerBillingService,
            IPaymentEmailService paymentEmailService,
            IUrlHelperFactory urlHelperFactory,
            Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor)
        {
            _userManager = userManager;
            _ownerBillingService = ownerBillingService;
            _paymentEmailService = paymentEmailService;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
        }

        private string AbsoluteUrl(string action, object? routeValues = null)
        {
            var actionContext = _actionContextAccessor.ActionContext!;
            var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
            return urlHelper.Action(action, "OwnerBilling", routeValues, actionContext.HttpContext.Request.Scheme)!;
        }

        private async Task<(ApplicationUser me, bool isSuperAdmin)> GetCallerInfoAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            var isSuperAdmin = User.IsInRole("SuperAdmin");
            return (me!, isSuperAdmin);
        }

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var (me, isSuperAdmin) = await GetCallerInfoAsync();
            if (me?.BuildingId != buildingId && !isSuperAdmin) return Forbid();

            var rows = await _ownerBillingService.GetIndexRowsAsync(buildingId.Value);
            
            ViewData["BuildingId"] = buildingId;
            return View(rows);
        }

        public async Task<IActionResult> View(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return NotFound();

            var (me, isSuperAdmin) = await GetCallerInfoAsync();
            if (me?.BuildingId == null && !isSuperAdmin) return Forbid();

            var restrictToBuildingId = (User.IsInRole("President")) ? me?.BuildingId : null;
            var page = await _ownerBillingService.GetBillsPageAsync(ownerId, restrictToBuildingId);
            
            if (page == null) return NotFound("No bills found for this owner.");

            return View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(RecordOwnerPaymentVM vm)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (me, isSuperAdmin) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var restrictToBuildingId = (User.IsInRole("President")) ? me.BuildingId : null;

            var (success, message, created) = await _ownerBillingService.PayAsync(
                vm.OwnerId, vm.CommonBillId, vm, restrictToBuildingId);

            if (!success)
            {
                if (message == "No allocation found for this owner & bill.") return NotFound(message);
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
            }

            if (created.Count > 0)
            {
                await _paymentEmailService.SendOwnerPaymentEmailAsync(vm.OwnerId, created, id => AbsoluteUrl(nameof(Receipt), new { id }));
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest();

            var (me, isSuperAdmin) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var restrictToBuildingId = (User.IsInRole("President")) ? me.BuildingId : null;

            var (success, message, created) = await _ownerBillingService.PayAllAsync(
                ownerId, restrictToBuildingId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { ownerId });
            }

            await _paymentEmailService.SendOwnerPaymentEmailAsync(ownerId, created, id => AbsoluteUrl(nameof(Receipt), new { id }));

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(string ownerId, Guid commonBillId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || commonBillId == Guid.Empty)
                return BadRequest();

            var (me, isSuperAdmin) = await GetCallerInfoAsync();
            if (me == null) return Forbid();

            var restrictToBuildingId = (User.IsInRole("President")) ? me.BuildingId : null;

            var (success, message, created) = await _ownerBillingService.FullPayAsync(
                ownerId, commonBillId, restrictToBuildingId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { ownerId });
            }

            await _paymentEmailService.SendOwnerPaymentEmailAsync(ownerId, created, id => AbsoluteUrl(nameof(Receipt), new { id }));

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId });
        }

        public async Task<IActionResult> Receipt(Guid id)
        {
            var (payment, buildingId) = await _ownerBillingService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (User.IsInRole("President") && me?.BuildingId != buildingId) return Forbid();

            var vm = new ReceiptViewModel
            {
                Id = payment.Id,
                ReceiptNo = "RC-" + payment.Id.ToString().Substring(0, 8).ToUpper(),
                PaidOn = payment.PaymentDate,
                OwnerName = payment.ExpenseAllocation?.Owner?.Fullname ?? payment.ExpenseAllocation?.Owner?.UserName!,
                OwnerEmail = payment.ExpenseAllocation?.Owner?.Email,
                BillTitle = payment.ExpenseAllocation?.CommonBill?.Name,
                BillDate = payment.ExpenseAllocation?.CommonBill?.BillDate ?? DateTime.MinValue,
                Amount = payment.Amount,
                Reference = payment.Reference,
                BuildingName = payment.ExpenseAllocation?.CommonBill?.Building?.Name ?? "(building)",
                FlatNumber = "-"
            };
            return View("~/Views/Shared/Receipt.cshtml", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailReceipt(Guid id)
        {
            var (payment, buildingId) = await _ownerBillingService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var me = await _userManager.GetUserAsync(User);
            if (User.IsInRole("President") && me?.BuildingId != buildingId) return Forbid();

            await _paymentEmailService.SendOwnerPaymentEmailAsync(payment.OwnerId, [payment], id => AbsoluteUrl(nameof(Receipt), new { id }));
            
            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { ownerId = payment.OwnerId });
        }
    }
}
