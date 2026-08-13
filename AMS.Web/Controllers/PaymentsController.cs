using AMS.Application.Features.Payments.Commands;
using AMS.Application.Features.Payments.Queries;
using AMS.Application.Mediator;
using AMS.Domain.Constants;
using AMS.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

[Authorize]
[Route("payments")]
public class PaymentsController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IMediator _mediator;
    private readonly ILogger<PaymentsController> _log;

    public PaymentsController(
        UserManager<ApplicationUser> userManager,
        IMediator mediator,
        ILogger<PaymentsController> log)
    {
        _userManager = userManager;
        _mediator = mediator;
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

        var (success, message, url) = await _mediator.Send(new CreateTenantCheckoutSessionCommand(
            billId, me.Id, amount, successUrl, cancelUrl));

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

        var isAdmin = User.IsInRole(Roles.President) || User.IsInRole(Roles.SuperAdmin);
        var ownerId = isAdmin ? Request.Form["ownerId"].FirstOrDefault() ?? me.Id : me.Id;

        var successUrl = Url.Action(nameof(Success), "Payments", null, Request.Scheme) + "?session_id={CHECKOUT_SESSION_ID}";
        var cancelUrl = Url.Action(nameof(Cancel), "Payments", null, Request.Scheme)!;

        var (success, message, url) = await _mediator.Send(new CreateOwnerCheckoutSessionCommand(
            commonBillId, ownerId, successUrl, cancelUrl));

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
        var vm = await _mediator.Send(new GetCheckoutResultQuery(sessionId));
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
            await _mediator.Send(new ProcessStripeWebhookCommand(json, sig));
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
