using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rezerv.Payroll.Application.Abstractions;
using Rezerv.Payroll.Infrastructure.Persistence;

namespace Rezerv.Payroll.Infrastructure;

public static class DependencyInjection
{
    /// <summary>MySQL 8.0 is the supported target; pinned (not auto-detected) so migrations can be generated without a live DB.</summary>
    public static readonly MySqlServerVersion MySqlVersion = new(new Version(8, 0, 36));

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Payroll")
            ?? throw new InvalidOperationException("ConnectionStrings:Payroll is not configured.");

        services.AddDbContext<PayrollDbContext>(options =>
            options.UseMySql(connectionString, MySqlVersion, mysql =>
            {
                mysql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(5), errorNumbersToAdd: null);
                mysql.MigrationsAssembly(typeof(PayrollDbContext).Assembly.FullName);
            }));

        services.AddScoped<IPayrollRepository, EfPayrollRepository>();
        return services;
    }

    /// <summary>Applies pending migrations and seeds sample data. Called once at API startup.</summary>
    public static async Task InitialiseDatabaseAsync(this IServiceProvider services, CancellationToken ct = default)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<PayrollDbContext>();
        await db.Database.MigrateAsync(ct);
        await DbSeeder.SeedAsync(db, ct);
    }
}
