using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StudentJobHub.Api.Data;
using StudentJobHub.Api.Models;
using StudentJobHub.Api.Services;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// DATABASE
// ============================================================

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection")));

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
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],

            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    builder.Configuration["Jwt:Key"]!))
        };
    });

// ============================================================
// AUTHORIZATION
// ============================================================

builder.Services.AddAuthorization();

// ============================================================
// APPLICATION SERVICES
// ============================================================

builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<ServiceService>();
builder.Services.AddScoped<JobService>();
builder.Services.AddScoped<JobApplicationService>();
builder.Services.AddScoped<ReviewService>();
builder.Services.AddScoped<NotificationService>();

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
    options.AddPolicy("ClientPolicy", policy =>
    {
        policy
            .WithOrigins(
                "http://localhost:5289",
                "http://localhost:7286")
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
}

// ============================================================
// HTTP REQUEST PIPELINE
// ============================================================

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors("ClientPolicy");

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

app.MapHub<StudentJobHub.Api.Hubs.NotificationHub>(
    "/hubs/notifications");

app.Run();