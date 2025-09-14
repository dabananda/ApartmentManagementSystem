using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.ViewModels.Others;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ApartmentManagementSystem.Controllers
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
            // Logged in users redirect to dashboard
            //if (User.Identity?.IsAuthenticated == true)
            //{
            //    if (User.IsInRole("SuperAdmin")) return RedirectToAction("Dashboard", "SuperAdmin");
            //    if (User.IsInRole("President")) return RedirectToAction("Dashboard", "President");
            //    if (User.IsInRole("Owner")) return RedirectToAction("Dashboard", "Owner");
            //    if (User.IsInRole("Tenant")) return RedirectToAction("Dashboard", "TenantPortal");
            //}
            return View(new ContactViewModel());
        }

        [Authorize]
        [HttpGet("/dashboard")]
        public IActionResult Dashboard()
        {
            if (User.IsInRole("SuperAdmin")) return RedirectToAction("Dashboard", "SuperAdmin");
            if (User.IsInRole("President")) return RedirectToAction("Dashboard", "President");
            if (User.IsInRole("Owner")) return RedirectToAction("Dashboard", "Owner");
            if (User.IsInRole("Tenant")) return RedirectToAction("Dashboard", "TenantPortal");

            // fallback: if no role matched, just go to home
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

             await _emailSender.SendEmailAsync("satyajitroy2k24@gmail.com", $"[AMS] {model.Subject}",
                 $"From: {model.Name} <{model.Email}>\n\n{model.Message}");

            _logger.LogInformation("Contact message from {Name} <{Email}>: {Subject} / {Message}",
                model.Name, model.Email, model.Subject, model.Message);

            TempData["ContactStatus"] = "sent";
            return RedirectToAction(nameof(Index));
        }

        [AllowAnonymous]
        public IActionResult Privacy() => View();

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
