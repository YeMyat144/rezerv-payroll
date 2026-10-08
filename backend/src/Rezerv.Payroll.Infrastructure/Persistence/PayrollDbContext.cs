using Microsoft.EntityFrameworkCore;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Infrastructure.Persistence;

public class PayrollDbContext(DbContextOptions<PayrollDbContext> options) : DbContext(options)
{
    public DbSet<Studio> Studios => Set<Studio>();
    public DbSet<Instructor> Instructors => Set<Instructor>();
    public DbSet<StudioClass> Classes => Set<StudioClass>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<ManualAdjustment> ManualAdjustments => Set<ManualAdjustment>();
    public DbSet<PayrollRun> PayrollRuns => Set<PayrollRun>();
    public DbSet<PayrollEntry> PayrollEntries => Set<PayrollEntry>();
    public DbSet<PayrollClassLine> PayrollClassLines => Set<PayrollClassLine>();
    public DbSet<PayrollLineItem> PayrollLineItems => Set<PayrollLineItem>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(PayrollDbContext).Assembly);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        // Money everywhere: 12 integer digits, 2 decimals. Rates are configured explicitly (4 decimals).
        configurationBuilder.Properties<decimal>().HavePrecision(12, 2);
        configurationBuilder.Properties<decimal?>().HavePrecision(12, 2);
        configurationBuilder.Properties<string>().HaveMaxLength(256);
        // Store enums as readable strings — payroll data is audited by humans.
        configurationBuilder.Properties<Enum>().HaveConversion<string>().HaveMaxLength(32);
    }
}
