using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace OneMoi.Infrastructure.Persistence;

/// <summary>
/// Used only by "dotnet ef migrations …" / "dotnet ef database update".
/// Reads the connection string from OneMoi.Api/appsettings(.Development).json or the
/// ConnectionStrings__OneMoi environment variable — so migrations work without starting the API.
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var apiDir = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "OneMoi.Api"));
        if (!Directory.Exists(apiDir)) apiDir = Directory.GetCurrentDirectory();
        var config = new ConfigurationBuilder()
            .SetBasePath(apiDir)
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var cs = config.GetConnectionString("OneMoi");
        if (string.IsNullOrWhiteSpace(cs))
            throw new InvalidOperationException("Set ConnectionStrings:OneMoi in OneMoi.Api/appsettings.Development.json or the ConnectionStrings__OneMoi environment variable.");

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(cs)
            .UseSnakeCaseNamingConvention()
            .Options;
        return new AppDbContext(options);
    }
}
