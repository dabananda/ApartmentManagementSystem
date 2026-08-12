using ApartmentManagementSystem.Domain.Entities;
using ApartmentManagementSystem.Infrastructure.BackgroundJobs;
using ApartmentManagementSystem.Infrastructure.Data;
using ApartmentManagementSystem.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Stripe;

namespace ApartmentManagementSystem.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

        // Note: AddDatabaseDeveloperPageExceptionFilter is usually added in the Web project for development

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

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();

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

        // Register all repositories automatically
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var repositoryTypes = assembly.GetTypes()
            .Where(t => t.Name.EndsWith("Repository") && !t.IsAbstract && !t.IsInterface);

        foreach (var repoType in repositoryTypes)
        {
            var interfaceType = repoType.GetInterfaces().FirstOrDefault(i => i.Name == $"I{repoType.Name}");
            if (interfaceType != null)
            {
                services.AddScoped(interfaceType, repoType);
            }
        }

        return services;
    }
}
