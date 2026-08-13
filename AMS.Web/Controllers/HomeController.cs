using System.Diagnostics;
using AMS.Application.Features.Home.DTOs;
using AMS.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace AMS.Web.Controllers;

public class HomeController : Controller
{
    private readonly ILogger<HomeController> _logger;
    private readonly IEmailSender _emailSender;
    private readonly IConfiguration _config;

    public HomeController(ILogger<HomeController> logger, IEmailSender emailSender, IConfiguration config)
    {
        _logger = logger;
        _emailSender = emailSender;
        _config = config;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        return View(new ContactViewModel());
    }

    [Authorize]
    [HttpGet("/dashboard")]
    public IActionResult Dashboard()
    {
        if (User.IsInRole(Roles.SuperAdmin)) return RedirectToAction("Dashboard", "SuperAdmin");
        if (User.IsInRole(Roles.President)) return RedirectToAction("Dashboard", "President");
        if (User.IsInRole(Roles.Owner)) return RedirectToAction("Dashboard", "Owner");
        if (User.IsInRole(Roles.Tenant)) return RedirectToAction("Dashboard", "TenantPortal");

        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Contact(ContactViewModel model)
    {
        if (!ModelState.IsValid)
        {
            return View("Index", model);
        }

        try
        {
            var destEmail = _config["ContactDestinationEmail"] ?? "dabananda.dev@gmail.com";
            await _emailSender.SendEmailAsync(destEmail, $"[AMS] {model.Subject}",
                $"From: {model.Email} <br/> Name: {model.Name} <br/> Message: {model.Message}");

            _logger.LogInformation("Contact message from {Name} <{Email}>: {Subject} / {Message}",
                model.Name, model.Email, model.Subject, model.Message);

            TempData["Success"] = "Your message has been sent successfully! We will get back to you shortly.";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send contact email.");
            TempData["Error"] = "Sorry, there was a problem sending your message. Please try again later.";
        }
        
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    public IActionResult Privacy() => View();

    [Route("/Home/Error/{statusCode}")]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error(int? statusCode = null)
    {
        var exceptionHandlerPathFeature = HttpContext.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerPathFeature>();

        if (exceptionHandlerPathFeature?.Error != null)
        {
            _logger.LogError(exceptionHandlerPathFeature.Error, "Unhandled exception occurred at {Path}", exceptionHandlerPathFeature.Path);
            statusCode ??= 500;
        }

        if (statusCode.HasValue)
        {
            if (statusCode.Value == 404) return View("Error404");
            if (statusCode.Value == 403) return View("Error403");
        }

        return View("Error", new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
