using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OneMoi.Api.Controllers;
using OneMoi.Api.Infrastructure;
using OneMoi.Api.Security;
using OneMoi.Application;
using OneMoi.Application.Common;
using OneMoi.Infrastructure;
using OneMoi.Infrastructure.Persistence;
using OneMoi.Infrastructure.Persistence.Seed;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;
config["Storage:Root"] ??= Path.Combine(builder.Environment.ContentRootPath, "wwwroot");

// Hosting (Render / Docker) tells us which port to listen on via $PORT
if (Environment.GetEnvironmentVariable("PORT") is { Length: > 0 } port)
    builder.WebHost.UseUrls($"http://0.0.0.0:{port}");

// Secrets are NOT stored in GitHub. Fail fast with a clear message if one is missing.
if (string.IsNullOrWhiteSpace(config.GetConnectionString("OneMoi")))
    throw new InvalidOperationException("Missing ConnectionStrings:OneMoi. Locally: appsettings.Development.json. On Render: environment variable ConnectionStrings__OneMoi.");
if ((config["Jwt:Key"] ?? "").Length < 32)
    throw new InvalidOperationException("Jwt:Key must be at least 32 characters. Locally: appsettings.Development.json. On Render: environment variable Jwt__Key.");
if ((config["App:OtpSecret"] ?? "").Length < 16)
    throw new InvalidOperationException("App:OtpSecret must be at least 16 characters. On Render: environment variable App__OtpSecret.");

// Behind the hosting proxy the real client IP is in X-Forwarded-For (needed for rate limiting + audit IPs)
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

// ── Layers ──
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUser, CurrentUser>();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(config);

// ── 1. AUTHENTICATION: who are you? (JWT bearer tokens) ──
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(o =>
{
    o.MapInboundClaims = false;
    o.TokenValidationParameters = new TokenValidationParameters
    {
        ValidIssuer = config["Jwt:Issuer"], ValidAudience = config["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Key"]!)),
        ValidateIssuer = true, ValidateAudience = true, ValidateLifetime = true, ValidateIssuerSigningKey = true,
        ClockSkew = TimeSpan.FromMinutes(1), RoleClaimType = System.Security.Claims.ClaimTypes.Role, NameClaimType = "name"
    };
});

// ── 2. ROLE-BASED AUTHORIZATION: what may you do? ([HasPermission(...)] on every endpoint) ──
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
builder.Services.AddScoped<IAuthorizationHandler, PermissionHandler>();
builder.Services.AddSingleton<IAuthorizationMiddlewareResultHandler, JsonAuthorizationResultHandler>();

// ── 3. Brute-force protection on login / OTP endpoints ──
builder.Services.AddRateLimiter(o =>
{
    o.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    o.OnRejected = (ctx, ct) => new ValueTask(ctx.HttpContext.Response.WriteAsJsonAsync(
        new { message = "Too many attempts. Please wait a minute and try again.", code = "RATE_LIMITED" }, ct));
    string Ip(HttpContext c) => c.Connection.RemoteIpAddress?.ToString() ?? "unknown";
    o.AddPolicy(RateLimits.Login, c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = config.GetValue("RateLimit:LoginPerMinute", 30), Window = TimeSpan.FromMinutes(1) }));
    o.AddPolicy(RateLimits.Otp, c => RateLimitPartition.GetFixedWindowLimiter(Ip(c),
        _ => new FixedWindowRateLimiterOptions { PermitLimit = config.GetValue("RateLimit:OtpPerMinute", 10), Window = TimeSpan.FromMinutes(1) }));
});

builder.Services.AddCors(o => o.AddPolicy("web", p => p
    .WithOrigins(config.GetSection("Cors:Origins").Get<string[]>() ?? new[] { "http://localhost:4200" })
    .AllowAnyHeader().AllowAnyMethod().WithExposedHeaders("Content-Disposition")));

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "OneMoi API", Version = "v1", Description = "One Mobile. One Moi Identity." });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme { Type = SecuritySchemeType.Http, Scheme = "bearer", BearerFormat = "JWT", In = ParameterLocation.Header });
    c.AddSecurityRequirement(doc => new OpenApiSecurityRequirement { [new OpenApiSecuritySchemeReference("Bearer", doc)] = [] });
});

var app = builder.Build();

// ── Create / upgrade database and load demo data ──
if (config.GetValue("Database:AutoMigrate", true))
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
    if (config.GetValue("Database:SeedDemoData", true))
        await scope.ServiceProvider.GetRequiredService<DbSeeder>().SeedAsync();
}

app.UseForwardedHeaders();
app.UseMiddleware<ErrorHandlingMiddleware>();
app.Use(async (ctx, next) =>   // basic security headers
{
    ctx.Response.Headers["X-Content-Type-Options"] = "nosniff";
    ctx.Response.Headers["X-Frame-Options"] = "DENY";
    ctx.Response.Headers["Referrer-Policy"] = "no-referrer";
    await next();
});
if (app.Environment.IsDevelopment() || config.GetValue("Swagger:Enabled", false))
{
    app.UseSwagger();
    app.UseSwaggerUI(c => c.DocumentTitle = "OneMoi API");
}
if (!string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase))
{
    // Local disk uploads (development). In the cloud, logos are stored in Supabase Storage.
    Directory.CreateDirectory(config["Storage:Root"]!);
    app.UseStaticFiles(new StaticFileOptions
    {
        FileProvider = new Microsoft.Extensions.FileProviders.PhysicalFileProvider(config["Storage:Root"]!)
    });
}
app.UseCors("web");
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<SessionValidationMiddleware>();   // ── 4. still active? same password? vendor not suspended?
app.UseAuthorization();
app.MapControllers();

// Health check used by Render (and by you): is the API up, and can it reach the database?
app.MapGet("/health", async (AppDbContext db, CancellationToken ct) =>
    await db.Database.CanConnectAsync(ct)
        ? Results.Ok(new { status = "ok", database = "ok", time = DateTime.UtcNow })
        : Results.Json(new { status = "degraded", database = "unreachable" }, statusCode: 503));
app.MapGet("/", () => Results.Redirect("/swagger"));
app.Run();
