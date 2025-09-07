namespace ApartmentManagementSystem.Services
{
    public interface IEmailSenderService
    {
        Task SendAsync(string toEmail, string subject, string htmlBody, string? fromName = null, string? fromAddress = null, CancellationToken ct = default);
    }
}
