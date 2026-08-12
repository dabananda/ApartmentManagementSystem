using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Application.Features.Owner.Services;
using AMS.Application.Features.Owner.DTOs;
using AMS.Web.Features.Shared;
using AMS.Application.Features.Tenancy.DTOs;
using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AMS.Web.Features.Owner
{
    [Authorize(Roles = Roles.PresidentOrSuperAdmin)]
    public class OwnerBillingController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IOwnerBillingService _ownerBillingService;
        private readonly IPaymentEmailService _paymentEmailService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;

        public OwnerBillingController(
            UserManager<ApplicationUser> userManager,
            IOwnerBillingService ownerBillingService,
            IPaymentEmailService paymentEmailService,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor)
        {
            _userManager = userManager;
            _ownerBillingService = ownerBillingService;
            _paymentEmailService = paymentEmailService;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
        }

        public async Task<IActionResult> Index(Guid? buildingId)
        {
            if (buildingId == null) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx?.BuildingId != buildingId && !ctx!.IsSuperAdmin) return Forbid();

            var rows = await _ownerBillingService.GetIndexRowsAsync(buildingId.Value);

            ViewData["BuildingId"] = buildingId;
            return View(rows);
        }

        public new async Task<IActionResult> View(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx?.BuildingId == null && !ctx!.IsSuperAdmin) return Forbid();

            var restrictToBuildingId = User.IsInRole(Roles.President) ? ctx?.BuildingId : null;
            var page = await _ownerBillingService.GetBillsPageAsync(ownerId, restrictToBuildingId);

            if (page == null) return NotFound("No bills found for this owner.");

            return View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(RecordOwnerPaymentVM vm)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var restrictToBuildingId = User.IsInRole(Roles.President) ? ctx.BuildingId : null;

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
                await _paymentEmailService.SendOwnerPaymentEmailAsync(vm.OwnerId, created,
                    id => this.BuildAbsoluteUrl(_urlHelperFactory, _actionContextAccessor, nameof(Receipt), "OwnerBilling", new { id }));
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId = vm.OwnerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string ownerId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) return BadRequest();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var restrictToBuildingId = User.IsInRole(Roles.President) ? ctx.BuildingId : null;

            var (success, message, created) = await _ownerBillingService.PayAllAsync(ownerId, restrictToBuildingId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { ownerId });
            }

            await _paymentEmailService.SendOwnerPaymentEmailAsync(ownerId, created,
                id => this.BuildAbsoluteUrl(_urlHelperFactory, _actionContextAccessor, nameof(Receipt), "OwnerBilling", new { id }));

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(string ownerId, Guid commonBillId)
        {
            if (string.IsNullOrWhiteSpace(ownerId) || commonBillId == Guid.Empty)
                return BadRequest();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (ctx == null) return Forbid();

            var restrictToBuildingId = User.IsInRole(Roles.President) ? ctx.BuildingId : null;

            var (success, message, created) = await _ownerBillingService.FullPayAsync(
                ownerId, commonBillId, restrictToBuildingId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { ownerId });
            }

            await _paymentEmailService.SendOwnerPaymentEmailAsync(ownerId, created,
                id => this.BuildAbsoluteUrl(_urlHelperFactory, _actionContextAccessor, nameof(Receipt), "OwnerBilling", new { id }));

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { ownerId });
        }

        public async Task<IActionResult> Receipt(Guid id)
        {
            var (payment, buildingId) = await _ownerBillingService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (User.IsInRole(Roles.President) && ctx?.BuildingId != buildingId) return Forbid();

            var vm = new ReceiptViewModel
            {
                Id = payment.Id,
                ReceiptNo = $"RC-{payment.Id.ToString()[..8].ToUpper()}",
                PaidOn = payment.PaymentDate,
                OwnerName = payment.ExpenseAllocation?.Owner?.Fullname ?? payment.ExpenseAllocation?.Owner?.UserName!,
                OwnerEmail = payment.ExpenseAllocation?.Owner?.Email,
                BillTitle = payment.ExpenseAllocation?.CommonBill?.Name ?? string.Empty,
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

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (User.IsInRole(Roles.President) && ctx?.BuildingId != buildingId) return Forbid();

            await _paymentEmailService.SendOwnerPaymentEmailAsync(payment.OwnerId, [payment],
                id => this.BuildAbsoluteUrl(_urlHelperFactory, _actionContextAccessor, nameof(Receipt), "OwnerBilling", new { id }));

            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { ownerId = payment.OwnerId });
        }
    }
}
