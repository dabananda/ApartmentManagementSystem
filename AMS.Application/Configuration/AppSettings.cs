namespace AMS.Application.Configuration;

public class AppSettings
{
    public ConnectionStringsSettings ConnectionStrings { get; set; } = new();
    public SuperAdminSettings SuperAdmin { get; set; } = new();
    public CloudinarySettings Cloudinary { get; set; } = new();
    public SmtpSettings Smtp { get; set; } = new();
    public StripeSettings Stripe { get; set; } = new();
    public LoggingSettings Logging { get; set; } = new();
    public string AllowedHosts { get; set; } = string.Empty;
}

public class ConnectionStringsSettings
{
    public string DefaultConnection { get; set; } = string.Empty;
}

public class SuperAdminSettings
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CloudinarySettings
{
    public string CloudName { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string ApiSecret { get; set; } = string.Empty;
}

public class SmtpSettings
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public string From { get; set; } = string.Empty;
    public string User { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class StripeSettings
{
    public string SecretKey { get; set; } = string.Empty;
    public string WebhookSecret { get; set; } = string.Empty;
    public string Currency { get; set; } = string.Empty;
}

public class LoggingSettings
{
    public LogLevelSettings LogLevel { get; set; } = new();
}

public class LogLevelSettings
{
    public string Default { get; set; } = string.Empty;
    public string MicrosoftAspNetCore { get; set; } = string.Empty;
}
