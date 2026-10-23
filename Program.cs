using System.Text;
using DriveX.Constants;
using DriveX.Data;
using DriveX.Filters;
using DriveX.Options;
using DriveX.Services;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// ---------- Database ----------
// The connection string comes from user-secrets in development,
// so the DB password never reaches GitHub.
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "ConnectionStrings:DefaultConnection is missing. Set it with 'dotnet user-secrets set'.");

builder.Services.AddDbContext<DriveXDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// ---------- JWT ----------
var jwtSection = builder.Configuration.GetSection("Jwt");
var jwtSettings = jwtSection.Get<JwtSettings>() ?? new JwtSettings();

if (Encoding.UTF8.GetBytes(jwtSettings.Secret).Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:Secret is missing or shorter than 32 characters. Set it with 'dotnet user-secrets set'.");
}

builder.Services.Configure<JwtSettings>(jwtSection);

var authBuilder = builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Keep claim names exactly as they are in the token ("sub", "role", "userId").
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
            ValidateIssuer = false,   // Spring tokens carry no issuer or audience
            ValidateAudience = false,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero,
            NameClaimType = "sub",
            RoleClaimType = "role"    // makes [Authorize(Roles = "ADMIN")] work
        };
    })
    // Holds the Google sign-in result for a few minutes, until /oauth2/google/complete has read it.
    .AddCookie(AuthSchemes.External, options =>
    {
        options.Cookie.Name = "DriveX.External";
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
    });

// ---------- Google login ----------
// Same Google client as the Spring app. Set the two values with user-secrets,
// or through the GOOGLE_CLIENT_ID / GOOGLE_CLIENT_SECRET environment variables.
var googleClientId = builder.Configuration["Authentication:Google:ClientId"]
    ?? builder.Configuration["GOOGLE_CLIENT_ID"];
var googleClientSecret = builder.Configuration["Authentication:Google:ClientSecret"]
    ?? builder.Configuration["GOOGLE_CLIENT_SECRET"];
var frontendBaseUrl = (builder.Configuration["Frontend:BaseUrl"] ?? "http://localhost:5173").TrimEnd('/');

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    authBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;
        options.SignInScheme = AuthSchemes.External;

        // Spring's default redirect URI, so nothing has to change in Google Cloud Console.
        options.CallbackPath = "/login/oauth2/code/google";

        // Lets us confirm that Google verified the email address.
        options.ClaimActions.MapJsonKey("email_verified", "email_verified");

        // Lax lets the cookie survive Google's redirect back, also over plain http on localhost.
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;

        // The user pressed "Cancel" at Google, or Google reported an error.
        options.Events.OnRemoteFailure = context =>
        {
            context.Response.Redirect($"{frontendBaseUrl}/?error=google_login_failed");
            context.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

builder.Services.Configure<FrontendSettings>(builder.Configuration.GetSection("Frontend"));

builder.Services.AddAuthorization();

// ---------- CORS (same origins as the Spring SecurityConfig) ----------
const string frontendCorsPolicy = "Frontend";

builder.Services.AddCors(options =>
{
    options.AddPolicy(frontendCorsPolicy, policy => policy
        .WithOrigins(
            "http://localhost:5173",
            "http://localhost:5174",
            "http://localhost:5175",
            "http://localhost:5176",
            "http://localhost:5180")
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowCredentials());
});

// ---------- Our services ----------
builder.Services.Configure<SmtpSettings>(builder.Configuration.GetSection("Smtp"));
builder.Services.AddMemoryCache(); // counts wrong code guesses

builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IPasswordResetService, PasswordResetService>();

builder.Services
    .AddControllers(options =>
    {
        options.Filters.Add<ApiExceptionFilter>();
    })
    .ConfigureApiBehaviorOptions(options =>
    {
        // Validation failures look like they did in Spring: { "errors": { "email": "Invalid email format" } }
        options.InvalidModelStateResponseFactory = context =>
        {
            var errors = context.ModelState
                .Where(entry => entry.Value is { Errors.Count: > 0 })
                .ToDictionary(
                    entry => string.IsNullOrEmpty(entry.Key)
                        ? entry.Key
                        : char.ToLowerInvariant(entry.Key[0]) + entry.Key[1..],
                    entry => entry.Value!.Errors[0].ErrorMessage);

            return new BadRequestObjectResult(new { errors });
        };
    });

var app = builder.Build();

app.UseCors(frontendCorsPolicy);
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
