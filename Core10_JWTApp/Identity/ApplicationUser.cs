using Microsoft.AspNetCore.Identity;

namespace Core10_JWTApp.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public string? DisplayName { get; set; }
}
