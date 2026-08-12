using AMS.Domain.Constants;
using AMS.Domain.Entities;
using AMS.Web.Features.Shared;
using AMS.Application.Features.TenantBilling.Services;
using AMS.Application.Features.Tenancy.DTOs;
using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;

namespace AMS.Web.Features.TenantBilling
{
    [Authorize(Roles = Roles.OwnerOrPresidentOrSuperAdmin)]
    public class TenantRentController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ITenantRentService _tenantRentService;
        private readonly IPaymentEmailService _paymentEmailService;
        private readonly IUrlHelperFactory _urlHelperFactory;
        private readonly IActionContextAccessor _actionContextAccessor;
        private readonly ILogger<TenantRentController> _log;

        public TenantRentController(
            UserManager<ApplicationUser> userManager,
            ITenantRentService tenantRentService,
            IPaymentEmailService paymentEmailService,
            IUrlHelperFactory urlHelperFactory,
            IActionContextAccessor actionContextAccessor,
            ILogger<TenantRentController> log)
        {
            _userManager = userManager;
            _tenantRentService = tenantRentService;
            _paymentEmailService = paymentEmailService;
            _urlHelperFactory = urlHelperFactory;
            _actionContextAccessor = actionContextAccessor;
            _log = log;
        }

        public async Task<IActionResult> List()
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            var restrictToOwnerId = User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null;

            var tenants = await _tenantRentService.GetTenantRentListAsync(restrictToOwnerId);
            return View(tenants);
        }

        public new async Task<IActionResult> View(string tenantUserId)
        {
            var ctx = await this.GetCallerContextAsync(_userManager);

            if (User.IsInRole(Roles.Owner))
            {
                var allowed = await _tenantRentService.IsTenantVisibleToOwnerAsync(tenantUserId, ctx!.Me.Id);
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

            var ctx = await this.GetCallerContextAsync(_userManager);
            var restrictToOwnerId = User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null;

            var (success, message, created, tenantUserId) = await _tenantRentService.PayAsync(vm, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return !string.IsNullOrEmpty(tenantUserId)
                    ? RedirectToAction(nameof(View), new { tenantUserId })
                    : RedirectToAction(nameof(List));
            }

            if (created.Count > 0 && !string.IsNullOrEmpty(tenantUserId))
                await SafeSendEmailAsync(tenantUserId, created);

            TempData["Success"] = message;
            return !string.IsNullOrEmpty(tenantUserId)
                ? RedirectToAction(nameof(View), new { tenantUserId })
                : RedirectToAction(nameof(List));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> FullPay(Guid billId)
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            var restrictToOwnerId = User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null;

            var (success, message, created, tenantUserId) = await _tenantRentService.FullPayAsync(billId, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return !string.IsNullOrEmpty(tenantUserId)
                    ? RedirectToAction(nameof(View), new { tenantUserId })
                    : RedirectToAction(nameof(List));
            }

            if (created.Count > 0 && !string.IsNullOrEmpty(tenantUserId))
                await SafeSendEmailAsync(tenantUserId, created);

            TempData["Success"] = message;
            return !string.IsNullOrEmpty(tenantUserId)
                ? RedirectToAction(nameof(View), new { tenantUserId })
                : RedirectToAction(nameof(List));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> PayAll(string tenantUserId)
        {
            var ctx = await this.GetCallerContextAsync(_userManager);
            var restrictToOwnerId = User.IsInRole(Roles.Owner) ? ctx?.Me.Id : null;

            var (success, message, created, _) = await _tenantRentService.PayAllAsync(tenantUserId, restrictToOwnerId);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction(nameof(View), new { tenantUserId });
            }

            if (created.Count > 0)
                await SafeSendEmailAsync(tenantUserId, created);

            TempData["Success"] = message;
            return RedirectToAction(nameof(View), new { tenantUserId });
        }

        public async Task<IActionResult> Receipt(Guid id)
        {
            var (payment, ownerId) = await _tenantRentService.GetReceiptDataAsync(id);
            if (payment == null) return NotFound();

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (User.IsInRole(Roles.Owner) && ownerId != ctx?.Me.Id) return Forbid();

            var vm = new ReceiptViewModel
            {
                Id = payment.Id,
                ReceiptNo = $"RC-{payment.Id.ToString()[..8].ToUpper()}",
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

            var ctx = await this.GetCallerContextAsync(_userManager);
            if (User.IsInRole(Roles.Owner) && ownerId != ctx?.Me.Id) return Forbid();

            await SafeSendEmailAsync(payment.TenantBill!.TenantUserId, [payment]);

            TempData["Success"] = "Receipt email sent.";
            return RedirectToAction(nameof(View), new { tenantUserId = payment.TenantBill!.TenantUserId });
        }

        private async Task SafeSendEmailAsync(string tenantUserId, IEnumerable<TenantPayment> payments)
        {
            try
            {
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
