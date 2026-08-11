using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.TenantBilling.Services;
using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Home.ViewModels;
using ApartmentManagementSystem.Infrastructure.Services;
using ApartmentManagementSystem.Features.Tenancy.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;

namespace ApartmentManagementSystem.Features.TenantBilling
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantRentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantRentService _tenantRentService;
        private readonly IPaymentEmailService _paymentEmailService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor _actionContextAccessor;
        private readonly ILogger<TenantRentController> _log;

        public TenantRentController(
            UserManager<ApplicationUser> userManager,
            ITenantRentService tenantRentService,
            IPaymentEmailService paymentEmailService,
            IUrlHelperFactory urlHelperFactory,
            Microsoft.AspNetCore.Mvc.Infrastructure.IActionContextAccessor actionContextAccessor,
            ILogger<TenantRentController> log)
        {
            _userManager = userManager;
            _tenantRentService = tenantRentService;
            _paymentEmailService = paymentEmailService;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _log = log;
        }

        private string AbsoluteUrl(string action, object? routeValues = null)
        {
            var actionContext = _actionContextAccessor.ActionContext!;
            var urlHelper = _urlHelperFactory.GetUrlHelper(actionContext);
            return urlHelper.Action(action, "TenantRent", routeValues, actionContext.HttpContext.Request.Scheme)!;
        }

        private async Task<(ApplicationUser me, bool isOwner)> GetCallerInfoAsync()
        {
            var me = await _userManager.GetUserAsync(User);
            return (me!, User.IsInRole("Owner"));
        }

        public async Task<IActionResult> List()
        {
            var (me, isOwner) = await GetCallerInfoAsync();
            var restrictToOwnerId = isOwner ? me.Id : null;

            var tenants = await _tenantRentService.GetTenantRentListAsync(restrictToOwnerId);
            return View(tenants);
        }

        public async Task<IActionResult> View(string tenantUserId)
        {
            var (me, isOwner) = await GetCallerInfoAsync();

            if (isOwner)
            {
                var allowed = await _tenantRentService.IsTenantVisibleToOwnerAsync(tenantUserId, me.Id);
                if (!allowed) return Forbid();
            }

            await _tenantRentService.EnsureCurrentMonthBillsForTenantAsync(tenantUserId);

            var page = await _tenantRentService.GetTenantBillsPageAsync(tenantUserId);
            if (page == null) return NotFound("No bills.");

            ViewData["History"] = await _tenantRentService.GetTenantPaymentHistoryAsync(tenantUserId);
            return View(page);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Pay(RecordTenantPaymentVM vm)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);

            var (me, isOwner) = await GetCallerInfoAsync();
            var restrictToOwnerId = isOwner ? me.Id : null;

            var (success, message, created, tenantUserId) = await _tenantRentService.PayAsync(vm, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return !string.IsNullOrEmpty(tenantUserId) ? RedirectToAction(nameof(View), new { tenantUserId }) : RedirectToAction(nameof(List));
            }

            if (created.Count > 0 && !string.IsNullOrEmpty(tenantUserId))
            {
                await SafeSendEmailAsync(tenantUserId, created);
            }

            TempData["Success"] = message;
            return !string.IsNullOrEmpty(tenantUserId) ? RedirectToAction(nameof(View), new { tenantUserId }) : RedirectToAction(nameof(List));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(Guid billId)
        {
            var (me, isOwner) = await GetCallerInfoAsync();
            var restrictToOwnerId = isOwner ? me.Id : null;

            var (success, message, created, tenantUserId) = await _tenantRentService.FullPayAsync(billId, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return !string.IsNullOrEmpty(tenantUserId) ? RedirectToAction(nameof(View), new { tenantUserId }) : RedirectToAction(nameof(List));
            }

            if (created.Count > 0 && !string.IsNullOrEmpty(tenantUserId))
            {
                await SafeSendEmailAsync(tenantUserId, created);
            }

            TempData["Success"] = message;
            return !string.IsNullOrEmpty(tenantUserId) ? RedirectToAction(nameof(View), new { tenantUserId }) : RedirectToAction(nameof(List));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string tenantUserId)
        {
            var (me, isOwner) = await GetCallerInfoAsync();
            var restrictToOwnerId = isOwner ? me.Id : null;

            var (success, message, created, tId) = await _tenantRentService.PayAllAsync(tenantUserId, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { tenantUserId });
            }

            if (created.Count > 0)
            {
                await SafeSendEmailAsync(tenantUserId, created);
            }

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { tenantUserId });
        }

        public async Task<IActionResult> Receipt(Guid id)
        {
            var (payment, ownerId) = await _tenantRentService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var (me, isOwner) = await GetCallerInfoAsync();
            if (isOwner && ownerId != me.Id) return Forbid();

            var vm = new ReceiptViewModel
            {
                Id = payment.Id,
                ReceiptNo = "RC-" + payment.Id.ToString().Substring(0, 8).ToUpper(),
                PaidOn = payment.PaymentDate,
                OwnerName = payment.TenantBill!.TenantUser!.Fullname ?? payment.TenantBill!.TenantUser!.UserName!,
                OwnerEmail = payment.TenantBill!.TenantUser!.Email,
                BillTitle = payment.TenantBill.Title,
                BillDate = payment.TenantBill.BillDate,
                Amount = payment.Amount,
                Reference = payment.Reference,
                BuildingName = payment.TenantBill!.Flat!.Building!.Name,
                FlatNumber = payment.TenantBill!.Flat!.FlatNumber
            };
            return View("~/Views/Shared/Receipt.cshtml", vm);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> EmailReceipt(Guid id)
        {
            var (payment, ownerId) = await _tenantRentService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var (me, isOwner) = await GetCallerInfoAsync();
            if (isOwner && ownerId != me.Id) return Forbid();

            await SafeSendEmailAsync(payment.TenantBill!.TenantUserId, [payment]);
            
            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { tenantUserId = payment.TenantBill!.TenantUserId });
        }

        private async Task SafeSendEmailAsync(string tenantUserId, IEnumerable<TenantPayment> payments)
        {
            try
            {
                // We haven't implemented receipt URLs for tenants in PaymentEmailService yet, but that wasn't in original either.
                await _paymentEmailService.SendTenantPaymentEmailAsync(tenantUserId, payments);
            }
            catch (System.Net.Mail.SmtpException ex)
            {
                _log.LogWarning(ex, "SMTP failed while emailing rent receipt. Payment saved.");
                TempData["Warning"] = "Payment saved, but the receipt email could not be sent (SMTP issue).";
            }
            catch (System.Net.Sockets.SocketException ex)
            {
                _log.LogWarning(ex, "Socket error while emailing rent receipt. Payment saved.");
                TempData["Warning"] = "Payment saved, but the receipt email could not be sent (network issue).";
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Unexpected error while emailing rent receipt. Payment saved.");
                TempData["Warning"] = "Payment saved, but sending the receipt failed.";
            }
        }
    }
}
