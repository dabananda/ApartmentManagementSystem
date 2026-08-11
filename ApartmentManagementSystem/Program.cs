using ApartmentManagementSystem.Data;
using ApartmentManagementSystem.Features.Announcements.Repositories;
using ApartmentManagementSystem.Features.Announcements.Services;
using ApartmentManagementSystem.Features.Expenses.Repositories;
using ApartmentManagementSystem.Features.Expenses.Services;
using ApartmentManagementSystem.Features.EntryLogs.Repositories;
using ApartmentManagementSystem.Features.EntryLogs.Services;
using ApartmentManagementSystem.Features.Flats.Repositories;
using ApartmentManagementSystem.Features.Flats.Services;
using ApartmentManagementSystem.Features.Buildings.Repositories;
using ApartmentManagementSystem.Features.Buildings.Services;
using ApartmentManagementSystem.Features.Administration.Repositories;
using ApartmentManagementSystem.Features.Administration.Services;
using ApartmentManagementSystem.Features.President.Repositories;
using ApartmentManagementSystem.Features.President.Services;
using ApartmentManagementSystem.Features.Reports.Repositories;
using ApartmentManagementSystem.Features.Reports.Services;
using ApartmentManagementSystem.Features.Maintenance.Repositories;
using ApartmentManagementSystem.Features.Maintenance.Services;
using ApartmentManagementSystem.Features.Tenancy.Repositories;
using ApartmentManagementSystem.Features.Tenancy.Services;
using ApartmentManagementSystem.Models;
using ApartmentManagementSystem.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.EntityFrameworkCore;
using Stripe;
using System.Globalization;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString));
builder.Services.AddDatabaseDeveloperPageExceptionFilter();

builder.Services.AddControllersWithViews();

builder.Services.AddDefaultIdentity<ApplicationUser>(options =>
    {
        options.SignIn.RequireConfirmedAccount = true;
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<
    IUserClaimsPrincipalFactory<ApplicationUser>,
    ApplicationUserClaimsPrincipalFactory>();

builder.Services.AddScoped<SignInManager<ApplicationUser>, ApplicationSignInManager>();
builder.Services.AddTransient<IBuildingCodeGenerator, BuildingCodeGenerator>();
builder.Services.AddTransient<IEmailSender, EmailSender>();
builder.Services.AddSingleton<IActionContextAccessor, ActionContextAccessor>();
builder.Services.AddSingleton<IUrlHelperFactory, UrlHelperFactory>();
builder.Services.AddHostedService<TenantMonthlyBillGenerator>();
builder.Services.AddScoped<IPhotoUploadService, CloudinaryPhotoUploadService>();
builder.Services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
builder.Services.AddScoped<IAnnouncementService, AnnouncementService>();
builder.Services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
builder.Services.AddScoped<IMaintenanceService, MaintenanceService>();
builder.Services.AddScoped<IExpenseAllocationRepository, ExpenseAllocationRepository>();
builder.Services.AddScoped<IExpenseAllocationService, ExpenseAllocationService>();
builder.Services.AddScoped<ICommonBillRepository, CommonBillRepository>();
builder.Services.AddScoped<ICommonBillService, CommonBillService>();
builder.Services.AddScoped<IExpensePaymentRepository, ExpensePaymentRepository>();
builder.Services.AddScoped<IExpensePaymentService, ExpensePaymentService>();
builder.Services.AddScoped<ITenantDirectoryRepository, TenantDirectoryRepository>();
builder.Services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
builder.Services.AddScoped<IFlatBillingProfileRepository, FlatBillingProfileRepository>();
builder.Services.AddScoped<IFlatBillingProfileService, FlatBillingProfileService>();
builder.Services.AddScoped<ITenantAssignmentRepository, TenantAssignmentRepository>();
builder.Services.AddScoped<ITenantAssignmentService, TenantAssignmentService>();
builder.Services.AddScoped<IEntryLogRepository, EntryLogRepository>();
builder.Services.AddScoped<IEntryLogService, EntryLogService>();
builder.Services.AddScoped<IFlatRepository, FlatRepository>();
builder.Services.AddScoped<IFlatService, FlatService>();
builder.Services.AddScoped<IBuildingRepository, BuildingRepository>();
builder.Services.AddScoped<IBuildingService, BuildingService>();
builder.Services.AddScoped<ISuperAdminDashboardRepository, SuperAdminDashboardRepository>();
builder.Services.AddScoped<ISuperAdminDashboardService, SuperAdminDashboardService>();
builder.Services.AddScoped<IPresidentDashboardRepository, PresidentDashboardRepository>();
builder.Services.AddScoped<IPresidentDashboardService, PresidentDashboardService>();
builder.Services.AddScoped<IPresidentFinancialReportRepository, PresidentFinancialReportRepository>();
builder.Services.AddScoped<IPresidentFinancialReportService, PresidentFinancialReportService>();
builder.Services.AddScoped<IPresidentOccupancyReportRepository, PresidentOccupancyReportRepository>();
builder.Services.AddScoped<IPresidentOccupancyReportService, PresidentOccupancyReportService>();
builder.Services.AddScoped<IPresidentVisitorReportRepository, PresidentVisitorReportRepository>();
builder.Services.AddScoped<IPresidentVisitorReportService, PresidentVisitorReportService>();

builder.Services.AddSingleton(sp =>
{
    var cfg = sp.GetRequiredService<IConfiguration>();
    var key = cfg["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey missing");
    return new StripeClient(key);
});

builder.Services.Configure<StripeOptions>(builder.Configuration.GetSection("Stripe"));

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
