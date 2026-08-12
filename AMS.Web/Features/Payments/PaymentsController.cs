using AMS.Application.Interfaces.Payments;
using AMS.Domain.Entities;
using AMS.Web.Features.Payments;
using AMS.Application.Features.Home.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Features.Payments
{
    [Authorize]
    [Route("payments")]
    public class PaymentsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IStripePaymentService _stripePaymentService;
        private readonly ILogger<PaymentsController> _log;

        public PaymentsController(
            UserManager<ApplicationUser> userManager,
            IStripePaymentService stripePaymentService,
            ILogger<PaymentsController> log)
        {
            _userManager = userManager;
            _stripePaymentService = stripePaymentService;
            _log = log;
        }

        private async Task<ApplicationUser?> GetCallerAsync() => await _userManager.GetUserAsync(User);

        [HttpPost("tenant/checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TenantCheckout(Guid billId, decimal? amount = null)
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var successUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action(nameof(Cancel), "Payments", null, Request.Scheme)!;

            var (success, message, url) = await _stripePaymentService.CreateTenantCheckoutSessionAsync(
                billId, me.Id, amount, successUrl, cancelUrl);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("Bills", "TenantPortal");
            }

            return Redirect(url!);
        }

        [HttpPost("owner/checkout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> OwnerCheckout(Guid commonBillId)
        {
            var me = await GetCallerAsync();
            if (me == null) return Forbid();

            var isAdmin = User.IsInRole("President") || User.IsInRole("SuperAdmin");
            var ownerId = isAdmin ? Request.Form["ownerId"].FirstOrDefault() ?? me.Id : me.Id;

            var successUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
            var cancelUrl = Url.Action(nameof(Cancel), "Payments", null, Request.Scheme)!;

            var (success, message, url) = await _stripePaymentService.CreateOwnerCheckoutSessionAsync(
                commonBillId, ownerId, successUrl, cancelUrl);

            if (!success)
            {
                TempData["Error"] = message;
                return RedirectToAction("View", "OwnerBilling", new { ownerId });
            }

            return Redirect(url!);
        }

        [HttpGet("success")]
        public async Task<IActionResult> Success([FromQuery(Name = "session_id")] string sessionId)
        {
            var vm = await _stripePaymentService.GetCheckoutResultAsync(sessionId);
            return View(vm);
        }

        [HttpGet("cancel")]
        public IActionResult Cancel() => View();

        [AllowAnonymous]
        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook()
        {
            string json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var sig = Request.Headers["Stripe-Signature"].ToString();

            try
            {
                await _stripePaymentService.ProcessWebhookEventAsync(json, sig);
            }
            catch (Stripe.StripeException ex)
            {
                _log.LogWarning(ex, "Stripe webhook signature verification failed.");
                return BadRequest();
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "Error processing Stripe webhook.");
                return StatusCode(500);
            }

            return Ok();
        }
    }
}
