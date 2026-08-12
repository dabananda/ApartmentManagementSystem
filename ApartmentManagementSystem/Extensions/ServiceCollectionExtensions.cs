using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Infrastructure.BackgroundJobs;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Infrastructure.Services;
using ApartmentManagementSystem.Features.Administration.Repositories;
using ApartmentManagementSystem.Features.Administration.Services;
using ApartmentManagementSystem.Features.Announcements.Repositories;
using ApartmentManagementSystem.Features.Announcements.Services;
using ApartmentManagementSystem.Features.Buildings.Repositories;
using ApartmentManagementSystem.Features.Buildings.Services;
using ApartmentManagementSystem.Features.EntryLogs.Repositories;
using ApartmentManagementSystem.Features.EntryLogs.Services;
using ApartmentManagementSystem.Features.Expenses.Repositories;
using ApartmentManagementSystem.Features.Expenses.Services;
using ApartmentManagementSystem.Features.Flats.Repositories;
using ApartmentManagementSystem.Features.Flats.Services;
using ApartmentManagementSystem.Features.Maintenance.Repositories;
using ApartmentManagementSystem.Features.Maintenance.Services;
using ApartmentManagementSystem.Features.Owner.Repositories;
using ApartmentManagementSystem.Features.Owner.Services;
using ApartmentManagementSystem.Features.Payments;
using ApartmentManagementSystem.Features.Payments.Repositories;
using ApartmentManagementSystem.Features.Payments.Services;
using ApartmentManagementSystem.Features.President.Repositories;
using ApartmentManagementSystem.Features.President.Services;
using ApartmentManagementSystem.Features.Reports.Repositories;
using ApartmentManagementSystem.Features.Reports.Services;
using ApartmentManagementSystem.Features.Tenancy.Repositories;
using ApartmentManagementSystem.Features.Tenancy.Services;
using ApartmentManagementSystem.Features.TenantBilling.Repositories;
using ApartmentManagementSystem.Features.TenantBilling.Services;
using ApartmentManagementSystem.Features.TenantPortal.Repositories;
using ApartmentManagementSystem.Features.TenantPortal.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Stripe;

namespace ApartmentManagementSystem.Extensions
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Registers infrastructure-level services: database, identity, email, photo upload, Stripe, and background jobs.
        /// </summary>
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(connectionString));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<ApplicationUser>(options =>
            {
                options.SignIn.RequireConfirmedAccount = true;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, ApplicationUserClaimsPrincipalFactory>();
            services.AddScoped<SignInManager<ApplicationUser>, ApplicationSignInManager>();

            services.AddTransient<IBuildingCodeGenerator, BuildingCodeGenerator>();
            services.AddTransient<IEmailSender, EmailSender>();
            services.AddScoped<IPaymentEmailService, PaymentEmailService>();
            services.AddScoped<IPhotoUploadService, CloudinaryPhotoUploadService>();

            services.AddHostedService<TenantMonthlyBillGenerator>();

            services.AddSingleton(sp =>
            {
                var cfg = sp.GetRequiredService<IConfiguration>();
                var key = cfg["Stripe:SecretKey"] ?? throw new InvalidOperationException("Stripe:SecretKey missing");
                return new StripeClient(key);
            });
            services.Configure<StripeOptions>(configuration.GetSection("Stripe"));

            return services;
        }

        /// <summary>
        /// Registers all feature-layer repositories and services (one scoped pair per feature).
        /// </summary>
        public static IServiceCollection AddFeatureServices(this IServiceCollection services)
        {
            // ── Announcements ───────────────────────────────────────────────────────
            services.AddScoped<IAnnouncementRepository, AnnouncementRepository>();
            services.AddScoped<IAnnouncementService, AnnouncementService>();

            // ── Maintenance ─────────────────────────────────────────────────────────
            services.AddScoped<IMaintenanceTicketRepository, MaintenanceTicketRepository>();
            services.AddScoped<IMaintenanceService, MaintenanceService>();

            // ── Expenses ────────────────────────────────────────────────────────────
            services.AddScoped<IExpenseAllocationRepository, ExpenseAllocationRepository>();
            services.AddScoped<IExpenseAllocationService, ExpenseAllocationService>();
            services.AddScoped<ICommonBillRepository, CommonBillRepository>();
            services.AddScoped<ICommonBillService, CommonBillService>();
            services.AddScoped<IExpensePaymentRepository, ExpensePaymentRepository>();
            services.AddScoped<IExpensePaymentService, ExpensePaymentService>();

            // ── Tenancy ─────────────────────────────────────────────────────────────
            services.AddScoped<ITenantDirectoryRepository, TenantDirectoryRepository>();
            services.AddScoped<ITenantDirectoryService, TenantDirectoryService>();
            services.AddScoped<ITenantAssignmentRepository, TenantAssignmentRepository>();
            services.AddScoped<ITenantAssignmentService, TenantAssignmentService>();

            // ── Flats ───────────────────────────────────────────────────────────────
            services.AddScoped<IFlatBillingProfileRepository, FlatBillingProfileRepository>();
            services.AddScoped<IFlatBillingProfileService, FlatBillingProfileService>();
            services.AddScoped<IFlatRepository, FlatRepository>();
            services.AddScoped<IFlatService, FlatService>();

            // ── Entry logs ──────────────────────────────────────────────────────────
            services.AddScoped<IEntryLogRepository, EntryLogRepository>();
            services.AddScoped<IEntryLogService, EntryLogService>();

            // ── Buildings ───────────────────────────────────────────────────────────
            services.AddScoped<IBuildingRepository, BuildingRepository>();
            services.AddScoped<IBuildingService, BuildingService>();

            // ── Administration / Dashboard ──────────────────────────────────────────
            services.AddScoped<ISuperAdminDashboardRepository, SuperAdminDashboardRepository>();
            services.AddScoped<ISuperAdminDashboardService, SuperAdminDashboardService>();
            services.AddScoped<IUserManagementRepository, UserManagementRepository>();
            services.AddScoped<IUserManagementService, UserManagementService>();

            // ── President ───────────────────────────────────────────────────────────
            services.AddScoped<IPresidentDashboardRepository, PresidentDashboardRepository>();
            services.AddScoped<IPresidentDashboardService, PresidentDashboardService>();

            // ── Reports ─────────────────────────────────────────────────────────────
            services.AddScoped<IPresidentFinancialReportRepository, PresidentFinancialReportRepository>();
            services.AddScoped<IPresidentFinancialReportService, PresidentFinancialReportService>();
            services.AddScoped<IPresidentOccupancyReportRepository, PresidentOccupancyReportRepository>();
            services.AddScoped<IPresidentOccupancyReportService, PresidentOccupancyReportService>();
            services.AddScoped<IPresidentVisitorReportRepository, PresidentVisitorReportRepository>();
            services.AddScoped<IPresidentVisitorReportService, PresidentVisitorReportService>();
            services.AddScoped<IMaintenanceReportRepository, MaintenanceReportRepository>();
            services.AddScoped<IMaintenanceReportService, MaintenanceReportService>();

            // ── Owner ───────────────────────────────────────────────────────────────
            services.AddScoped<IOwnerRepository, OwnerRepository>();
            services.AddScoped<IOwnerService, OwnerService>();
            services.AddScoped<IOwnerBillingRepository, OwnerBillingRepository>();
            services.AddScoped<IOwnerBillingService, OwnerBillingService>();

            // ── Tenant billing & portal ─────────────────────────────────────────────
            services.AddScoped<ITenantRentRepository, TenantRentRepository>();
            services.AddScoped<ITenantRentService, TenantRentService>();
            services.AddScoped<ITenantPortalRepository, TenantPortalRepository>();
            services.AddScoped<ITenantPortalService, TenantPortalService>();

            // ── Payments ────────────────────────────────────────────────────────────
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<IStripePaymentService, StripePaymentService>();

            return services;
        }
    }
}
