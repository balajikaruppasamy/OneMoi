using Microsoft.Extensions.DependencyInjection;
using OneMoi.Application.Features.Auth;
using OneMoi.Application.Features.Dashboard;
using OneMoi.Application.Features.Functions;
using OneMoi.Application.Features.Individuals;
using OneMoi.Application.Features.Masters;
using OneMoi.Application.Features.Moi;
using OneMoi.Application.Features.Operators;
using OneMoi.Application.Features.Reports;
using OneMoi.Application.Features.Tenants;

namespace OneMoi.Application;

public static class DependencyInjection
{
    /// <summary>Registers all business services (one per feature folder).</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<AuthService>();
        services.AddScoped<TenantService>();
        services.AddScoped<MasterService>();
        services.AddScoped<FunctionService>();
        services.AddScoped<OperatorService>();
        services.AddScoped<MoiEntryService>();
        services.AddScoped<MyMoiService>();
        services.AddScoped<ReportService>();
        services.AddScoped<DashboardService>();
        return services;
    }
}
