using ApartmentManagementSystem.Domain.Constants;
using ApartmentManagementSystem.Features.Home.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ApartmentManagementSystem.Features.Home
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IEmailSender _emailSender;

        public HomeController(ILogger<HomeController> logger, IEmailSender emailSender)
        {
            _logger = logger;
            _emailSender = emailSender;
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
                TempData["ContactStatus"] = "error";
                return View("Index", model);
            }

            await _emailSender.SendEmailAsync("dabananda.dev@gmail.com", $"[AMS] {model.Subject}",
                $"From: {model.Email} <br/> Name: {model.Name} <br/> Message: {model.Message}");

            _logger.LogInformation("Contact message from {Name} <{Email}>: {Subject} / {Message}",
                model.Name, model.Email, model.Subject, model.Message);

            TempData["ContactStatus"] = "sent";
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
}
