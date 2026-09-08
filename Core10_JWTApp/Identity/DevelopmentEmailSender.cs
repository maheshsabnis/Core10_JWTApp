using System.Net;
using Microsoft.AspNetCore.Identity;

namespace Core10_JWTApp.Identity;

public sealed class DevelopmentEmailSender : IEmailSender<ApplicationUser>
{
    private readonly ILogger<DevelopmentEmailSender> _logger;

    public DevelopmentEmailSender(
        IHostEnvironment environment,
        ILogger<DevelopmentEmailSender> logger)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Configure a production IEmailSender<ApplicationUser> before running outside Development.");
        }

        _logger = logger;
    }

    public Task SendConfirmationLinkAsync(ApplicationUser user, string email, string confirmationLink)
    {
        _logger.LogInformation("Development email confirmation for {Email}: {ConfirmationLink}",
            email, WebUtility.HtmlDecode(confirmationLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetLinkAsync(ApplicationUser user, string email, string resetLink)
    {
        _logger.LogInformation("Development password reset for {Email}: {ResetLink}",
            email, WebUtility.HtmlDecode(resetLink));
        return Task.CompletedTask;
    }

    public Task SendPasswordResetCodeAsync(ApplicationUser user, string email, string resetCode)
    {
        _logger.LogInformation("Development password reset for {Email}: {ResetCode}", email, resetCode);
        return Task.CompletedTask;
    }
}
