using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Extensions;
using ApartmentManagementSystem.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();

builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();

// Encapsulated setup
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddFeatureServices();

var app = builder.Build();

// Configure the localization options - set currency from $ to tk
var customCulture = new CultureInfo("en-US");
customCulture.NumberFormat.CurrencySymbol = "tk";
customCulture.NumberFormat.CurrencyPositivePattern = 3;
customCulture.NumberFormat.CurrencyNegativePattern = 8;

var localizationOptions = new RequestLocalizationOptions
{
    DefaultRequestCulture = new RequestCulture(customCulture),
    SupportedCultures = new List<CultureInfo> { customCulture },
    SupportedUICultures = new List<CultureInfo> { customCulture }
};

app.UseRequestLocalization(localizationOptions);

if (app.Environment.IsDevelopment())
{
    app.UseMigrationsEndPoint();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var configuration = services.GetRequiredService<IConfiguration>();
        var superAdminPassword = configuration["SuperAdminPassword"];
        var superAdminEmail = configuration["SuperAdminEmail"];

        if (string.IsNullOrEmpty(superAdminPassword))
        {
            throw new InvalidOperationException("SuperAdminPassword not found in configuration.");
        }

        DbInitializer.Initialize(context, userManager, roleManager, superAdminEmail, superAdminPassword).Wait();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");
app.MapRazorPages();

app.Run();
