using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OneMoi.Application.Common;
using OneMoi.Infrastructure.Persistence;
using OneMoi.Infrastructure.Persistence.Seed;
using OneMoi.Infrastructure.Services;

namespace OneMoi.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Database (PostgreSQL) + technical services.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        services.AddDbContext<AppDbContext>(o => o
            .UseNpgsql(config.GetConnectionString("OneMoi"), pg => pg.EnableRetryOnFailure(5))
            .UseSnakeCaseNamingConvention());          // NameTa → name_ta, CreatedAt → created_at
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        services.AddSingleton(config.GetSection("App").Get<AppSettings>() ?? new AppSettings());
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();
        services.AddSingleton<ITokenService, JwtTokenService>();
        // Uploads: local disk in development, Supabase Storage in the cloud ("Storage:Provider": "Supabase")
        if (string.Equals(config["Storage:Provider"], "Supabase", StringComparison.OrdinalIgnoreCase))
            services.AddHttpClient<IFileStorage, SupabaseFileStorage>();
        else
            services.AddSingleton<IFileStorage, LocalFileStorage>();
        services.AddScoped<INotificationSender, NotificationSender>();
        services.AddMemoryCache();
        services.AddSingleton<ISessionStateStore, SessionStateStore>();
        services.AddHttpClient<ITransliterationService, TransliterationService>();
        services.AddScoped<DbSeeder>();
        return services;
    }
}
