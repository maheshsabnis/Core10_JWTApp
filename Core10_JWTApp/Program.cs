using Core10_JWTApp.Identity;
using Core10_JWTApp.Patients;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
// Registers OpenAPI document generation (Swagger-style JSON at /openapi/v1.json in Development),
// and adds a Bearer security scheme so tokens can be supplied via the Swagger UI "Authorize" button.
builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, cancellationToken) =>
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "Enter the access token returned by POST /auth/login."
        };
        document.Security ??= new List<OpenApiSecurityRequirement>();
        document.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", document)] = new List<string>()
        });
        return Task.CompletedTask;
    });
});
// Registers EF Core DbContext, ASP.NET Core Identity API endpoints, and bearer token authentication
// (see Identity/IdentityServiceExtensions.cs for the full configuration).
builder.Services.AddApplicationIdentity(builder.Configuration, builder.Environment);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // Ensure the Identity database/schema exists on startup (Development only, no migrations required).
    await using var scope = app.Services.CreateAsyncScope();
    var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await database.Database.EnsureCreatedAsync();

    // Expose the OpenAPI JSON document for local testing/tools (e.g. Swagger UI, Postman import).
    app.MapOpenApi();

    // Serve the Swagger UI page at /swagger, pointing it at the generated OpenAPI document
    // so all endpoints (Identity + Patients) can be browsed and tested from the browser.
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "Core10_JWTApp v1");
        options.RoutePrefix = "swagger";
    });
}

// Redirects HTTP requests to HTTPS.
app.UseHttpsRedirection();
// Resolves the caller's identity (bearer token) from the request; must run before UseAuthorization.
app.UseAuthentication();
// Enforces [Authorize]/RequireAuthorization() rules on endpoints based on the resolved identity.
app.UseAuthorization();

// Groups all ASP.NET Core Identity endpoints (register, login, refresh, confirmEmail, manage/info, etc.)
// under the "/auth" prefix.
var authentication = app.MapGroup("/auth").WithTags("Authentication");
authentication.MapIdentityApi<ApplicationUser>();

// Groups the custom Patients API under "/api"; MapPatientEndpoints adds the secured "/patients" routes.
var api = app.MapGroup("/api").WithTags("Patients");
api.MapPatientEndpoints();

// Starts the web server and blocks until the application shuts down.
app.Run();
