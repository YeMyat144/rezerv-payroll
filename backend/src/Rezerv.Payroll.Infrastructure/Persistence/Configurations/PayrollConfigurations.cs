using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Infrastructure.Persistence.Configurations;

public sealed class PayrollRunConfiguration : IEntityTypeConfiguration<PayrollRun>
{
    public void Configure(EntityTypeBuilder<PayrollRun> b)
    {
        b.ToTable("payroll_runs");
        b.HasKey(x => x.Id);
        b.Property(x => x.GeneratedAtUtc).HasColumnType("datetime(6)");
        b.Ignore(x => x.Period);
        b.Ignore(x => x.TotalPayout);
        // The database is the final arbiter of idempotency: two concurrent generate requests for the
        // same period cannot both succeed, regardless of application-level checks.
        b.HasIndex(x => new { x.StartDate, x.EndDate }).IsUnique().HasDatabaseName("ux_payroll_runs_period");
        b.HasMany(x => x.Entries).WithOne(e => e.PayrollRun).HasForeignKey(e => e.PayrollRunId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PayrollEntryConfiguration : IEntityTypeConfiguration<PayrollEntry>
{
    public void Configure(EntityTypeBuilder<PayrollEntry> b)
    {
        b.ToTable("payroll_entries");
        b.HasKey(x => x.Id);
        b.Property(x => x.InstructorName).IsRequired().HasMaxLength(128);
        b.Property(x => x.StudioName).IsRequired().HasMaxLength(128);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Ignore(x => x.ClassEarnings);
        b.HasOne(x => x.Instructor).WithMany().HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.PayrollRunId, x.InstructorId }).IsUnique();
        b.HasMany(x => x.ClassLines).WithOne(c => c.PayrollEntry).HasForeignKey(c => c.PayrollEntryId).OnDelete(DeleteBehavior.Cascade);
        b.HasMany(x => x.LineItems).WithOne(l => l.PayrollEntry).HasForeignKey(l => l.PayrollEntryId).OnDelete(DeleteBehavior.Cascade);
    }
}

public sealed class PayrollClassLineConfiguration : IEntityTypeConfiguration<PayrollClassLine>
{
    public void Configure(EntityTypeBuilder<PayrollClassLine> b)
    {
        b.ToTable("payroll_class_lines");
        b.HasKey(x => x.Id);
        b.Property(x => x.ClassName).IsRequired().HasMaxLength(128);
        b.Property(x => x.StartsAt).HasColumnType("datetime(6)");
        b.Ignore(x => x.Total);
        b.HasIndex(x => x.PayrollEntryId);
    }
}

public sealed class PayrollLineItemConfiguration : IEntityTypeConfiguration<PayrollLineItem>
{
    public void Configure(EntityTypeBuilder<PayrollLineItem> b)
    {
        b.ToTable("payroll_line_items");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(512);
        b.Property(x => x.ReferenceType).IsRequired().HasMaxLength(32);
        b.Property(x => x.Rate).HasPrecision(5, 4);
        b.HasIndex(x => x.PayrollEntryId);
        b.HasIndex(x => new { x.ReferenceType, x.ReferenceId });
    }
}
