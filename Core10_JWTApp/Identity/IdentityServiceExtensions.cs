using Microsoft.AspNetCore.Authentication.BearerToken;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Core10_JWTApp.Identity;

public static class IdentityServiceExtensions
{
    public static IServiceCollection AddApplicationIdentity(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var connectionString = configuration.GetConnectionString("IdentityDatabase");
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("Configure ConnectionStrings:IdentityDatabase for SQL Server.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));

        services.AddIdentityApiEndpoints<ApplicationUser>(options =>
        {
            // Skip email confirmation in Development since DevelopmentEmailSender only logs
            // confirmation links instead of sending real emails, which would otherwise block login.
            options.SignIn.RequireConfirmedEmail = !environment.IsDevelopment();
            options.User.RequireUniqueEmail = true;
            options.Lockout.MaxFailedAccessAttempts = 5;
            options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        })
        .AddEntityFrameworkStores<ApplicationDbContext>();

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = IdentityConstants.BearerScheme;
            options.DefaultChallengeScheme = IdentityConstants.BearerScheme;
        });
        services.AddAuthorization();

        services.AddOptions<BearerTokenOptions>(IdentityConstants.BearerScheme)
            .Configure(options =>
            {
                options.BearerTokenExpiration = TimeSpan.FromMinutes(
                    configuration.GetValue("IdentityTokens:AccessTokenMinutes", 15));
                options.RefreshTokenExpiration = TimeSpan.FromDays(
                    configuration.GetValue("IdentityTokens:RefreshTokenDays", 7));
            })
            .Validate(options => options.BearerTokenExpiration > TimeSpan.Zero,
                "Access token lifetime must be positive.")
            .Validate(options => options.RefreshTokenExpiration > options.BearerTokenExpiration,
                "Refresh token lifetime must exceed the access token lifetime.")
            .ValidateOnStart();

        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Replace the Development email sender registration with a production IEmailSender<ApplicationUser>.");
        }

        services.AddTransient<IEmailSender<ApplicationUser>, DevelopmentEmailSender>();
        return services;
    }
}
