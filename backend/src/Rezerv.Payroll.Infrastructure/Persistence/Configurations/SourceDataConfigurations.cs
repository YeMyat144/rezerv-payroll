using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Infrastructure.Persistence.Configurations;

public sealed class StudioConfiguration : IEntityTypeConfiguration<Studio>
{
    public void Configure(EntityTypeBuilder<Studio> b)
    {
        b.ToTable("studios");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.Currency).IsRequired().HasMaxLength(3);
        b.Property(x => x.MembershipCommissionRate).HasPrecision(5, 4);
        b.Property(x => x.PackageCommissionRate).HasPrecision(5, 4);
    }
}

public sealed class InstructorConfiguration : IEntityTypeConfiguration<Instructor>
{
    public void Configure(EntityTypeBuilder<Instructor> b)
    {
        b.ToTable("instructors");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.Email).HasMaxLength(256);
        b.HasOne(x => x.Studio).WithMany(s => s.Instructors).HasForeignKey(x => x.StudioId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => x.StudioId);
    }
}

public sealed class StudioClassConfiguration : IEntityTypeConfiguration<StudioClass>
{
    public void Configure(EntityTypeBuilder<StudioClass> b)
    {
        b.ToTable("classes");
        b.HasKey(x => x.Id);
        b.Property(x => x.Name).IsRequired().HasMaxLength(128);
        b.Property(x => x.StartsAt).HasColumnType("datetime(6)");
        b.Ignore(x => x.Date);
        b.HasOne(x => x.Studio).WithMany().HasForeignKey(x => x.StudioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Instructor).WithMany(i => i.Classes).HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
        // Payroll generation filters classes by date range per instructor.
        b.HasIndex(x => new { x.InstructorId, x.StartsAt });
        b.HasIndex(x => x.StartsAt);
    }
}

public sealed class BookingConfiguration : IEntityTypeConfiguration<Booking>
{
    public void Configure(EntityTypeBuilder<Booking> b)
    {
        b.ToTable("bookings");
        b.HasKey(x => x.Id);
        b.Property(x => x.CustomerName).IsRequired().HasMaxLength(128);
        b.HasOne(x => x.Class).WithMany(c => c.Bookings).HasForeignKey(x => x.ClassId).OnDelete(DeleteBehavior.Cascade);
        b.HasIndex(x => new { x.ClassId, x.Status });
    }
}

public sealed class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> b)
    {
        b.ToTable("sales");
        b.HasKey(x => x.Id);
        b.Property(x => x.Description).IsRequired().HasMaxLength(256);
        b.Property(x => x.CustomerName).HasMaxLength(128);
        b.Property(x => x.SoldAt).HasColumnType("datetime(6)");
        b.Property(x => x.RefundedAt).HasColumnType("datetime(6)");
        b.Ignore(x => x.IsRefunded);
        b.HasOne(x => x.Studio).WithMany().HasForeignKey(x => x.StudioId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Instructor).WithMany(i => i.Sales).HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.InstructorId, x.SoldAt });
        b.HasIndex(x => x.RefundedAt);
    }
}

public sealed class ManualAdjustmentConfiguration : IEntityTypeConfiguration<ManualAdjustment>
{
    public void Configure(EntityTypeBuilder<ManualAdjustment> b)
    {
        b.ToTable("manual_adjustments");
        b.HasKey(x => x.Id);
        b.Property(x => x.Reason).IsRequired().HasMaxLength(256);
        b.HasOne(x => x.Instructor).WithMany(i => i.ManualAdjustments).HasForeignKey(x => x.InstructorId).OnDelete(DeleteBehavior.Restrict);
        b.HasIndex(x => new { x.InstructorId, x.EffectiveDate });
    }
}
