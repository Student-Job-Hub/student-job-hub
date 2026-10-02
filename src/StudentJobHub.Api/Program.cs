using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.Models;
using StudentJobHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey) || Encoding.UTF8.GetByteCount(jwtKey) < 32)
{
    throw new InvalidOperationException(
        "Jwt:Key must be configured with at least 32 bytes.");
}

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"))
    .ConfigureWarnings(warnings => warnings.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

// ============================================================
// ASP.NET CORE IDENTITY
// ============================================================

builder.Services
    .AddIdentityCore<ApplicationUser>()
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ============================================================
// JWT AUTHENTICATION
// ============================================================

builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme =
            JwtBearerDefaults.AuthenticationScheme;

        options.DefaultChallengeScheme =
            JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.Events = new JwtBearerEvents
        {
            OnTokenValidated = async context =>
            {
                var userId = context.Principal?.FindFirst(
                    System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrWhiteSpace(userId))
                {
                    context.Fail("The token has no user identity.");
                    return;
                }

                var userManager = context.HttpContext.RequestServices
                    .GetRequiredService<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                if (user == null || await userManager.IsLockedOutAsync(user))
                {
                    context.Fail("The account is suspended or no longer exists.");
                }
            },
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) &&
                    context.HttpContext.Request.Path.StartsWithSegments("/hubs/messages"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            }
        };

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// ============================================================
// RATE LIMITING
// ============================================================

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, cancellationToken) =>
    {
        await context.HttpContext.Response.WriteAsJsonAsync(new
        {
            message = "Too many requests. Please try again later."
        }, cancellationToken);
    };

    options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
        CreateIpPartition(context, permitLimit: 120, window: TimeSpan.FromMinutes(1)));

    options.AddPolicy("login", context =>
        CreateIpPartition(context, permitLimit: 5, window: TimeSpan.FromMinutes(1)));
    options.AddPolicy("register", context =>
        CreateIpPartition(context, permitLimit: 10, window: TimeSpan.FromMinutes(1)));
    options.AddPolicy("refresh", context =>
        CreateIpPartition(context, permitLimit: 30, window: TimeSpan.FromMinutes(1)));
});

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<JobApplicationService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<NotificationService>();
builder.Services.AddScoped<EmailNotificationService>();
builder.Services.AddScoped<AdminService>();
builder.Services.AddScoped<AdminBootstrapService>();
builder.Services.AddScoped<JobBookmarkService>();
builder.Services.AddScoped<ServiceBookingService>();
builder.Services.AddScoped<ExportService>();
builder.Services.AddScoped<AuditLogService>();

// ============================================================
// CONTROLLERS
// ============================================================

builder.Services.AddControllers();

// ============================================================
// OPENAPI
// ============================================================

builder.Services.AddOpenApi();

// ============================================================
// SIGNALR
// ============================================================

builder.Services.AddSignalR();

// ============================================================
// CORS
// ============================================================

builder.Services.AddCors(options =>
{
    var allowedOrigins = builder.Configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? [];

    options.AddPolicy("ClientPolicy", policy =>
    {
        policy
            .WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// ============================================================
// BUILD APPLICATION
// ============================================================

var app = builder.Build();

// ============================================================
// AUTOMATIC MIGRATIONS & SEED DEFAULT ROLES
// ============================================================

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider
        .GetRequiredService<ApplicationDbContext>();

    await dbContext.Database.MigrateAsync();

    var roleManager = scope.ServiceProvider
        .GetRequiredService<RoleManager<IdentityRole>>();

    var roles = new[]
    {
        "Student",
        "Lecturer",
        "Business",
        "Admin"
    };

    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(
                new IdentityRole(role));
        }
    }

    var adminEmail = builder.Configuration["AdminBootstrap:Email"];
    var adminPassword = builder.Configuration["AdminBootstrap:Password"];

    if (!string.IsNullOrWhiteSpace(adminEmail) ||
        !string.IsNullOrWhiteSpace(adminPassword))
    {
        var adminBootstrap = scope.ServiceProvider
            .GetRequiredService<AdminBootstrapService>();
        await adminBootstrap.EnsureAdminAsync(adminEmail, adminPassword);
    }
}

// ============================================================
// HTTP REQUEST PIPELINE
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi().AllowAnonymous();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles();

app.UseRouting();

app.UseCors("ClientPolicy");

app.UseRateLimiter();

app.UseAuthentication();

app.UseAuthorization();

app.MapGet("/health", () => Results.Ok(new { status = "healthy" }))
    .AllowAnonymous();

app.MapControllers();

app.MapHub<StudentJobHub.Api.Hubs.NotificationHub>(
    "/hubs/notifications");

app.MapHub<StudentJobHub.Api.Hubs.DirectMessageHub>(
    "/hubs/messages");

app.Run();

static RateLimitPartition<string> CreateIpPartition(
    HttpContext context,
    int permitLimit,
    TimeSpan window)
{
    var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

    return RateLimitPartition.GetFixedWindowLimiter(clientIp, _ => new FixedWindowRateLimiterOptions
    {
        PermitLimit = permitLimit,
        Window = window,
        QueueLimit = 0,
        AutoReplenishment = true
    });
}