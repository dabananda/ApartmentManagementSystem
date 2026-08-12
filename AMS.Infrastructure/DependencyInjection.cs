using AMS.Domain.Entities;
using AMS.Infrastructure.BackgroundJobs;
using AMS.Infrastructure.Data;
using AMS.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stripe;
using AMS.Application.Configuration;

namespace AMS.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, AppSettings appSettings)
    {
        var connectionString = appSettings.ConnectionStrings.DefaultConnection;
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseSqlServer(connectionString));

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
            var settings = sp.GetRequiredService<AppSettings>();
            var key = settings.Stripe.SecretKey;
            if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("Stripe:SecretKey missing");
            return new StripeClient(key);
        });

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
