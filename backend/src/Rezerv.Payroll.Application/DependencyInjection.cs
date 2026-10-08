using Microsoft.Extensions.DependencyInjection;
using Rezerv.Payroll.Application.Payroll;
using Rezerv.Payroll.Domain.Services;

namespace Rezerv.Payroll.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<PayrollCalculator>();
        services.AddScoped<PayrollService>();
        return services;
    }
}
