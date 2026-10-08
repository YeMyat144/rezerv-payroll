using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Tests;

/// <summary>Small builders so each test reads like the business rule it checks.</summary>
internal static class TestData
{
    public static readonly PayrollPeriod June = new(new DateOnly(2026, 6, 1), new DateOnly(2026, 6, 30));
    public static readonly PayrollPeriod May = new(new DateOnly(2026, 5, 1), new DateOnly(2026, 5, 31));

    public static Studio Studio(NoShowPayoutPolicy policy = NoShowPayoutPolicy.NoPayout) => new()
    {
        Id = Guid.NewGuid(),
        Name = "Test Studio",
        Currency = "SGD",
        NoShowPayoutPolicy = policy,
        MembershipCommissionRate = 0.10m,
        PackageCommissionRate = 0.05m,
        AttendanceBonusThreshold = 15,
        AttendanceBonusAmount = 20m,
    };

    public static Instructor Instructor(Studio studio, string name = "John") => new()
    {
        Id = Guid.NewGuid(),
        StudioId = studio.Id,
        Studio = studio,
        Name = name,
    };

    public static StudioClass Class(
        Instructor instructor,
        string date = "2026-06-10",
        decimal fixedFee = 30m,
        decimal? perBooking = 10m,
        int attended = 0,
        int noShow = 0,
        int cancelled = 0,
        ClassStatus status = ClassStatus.Completed,
        string name = "Yoga")
    {
        var cls = new StudioClass
        {
            Id = Guid.NewGuid(),
            StudioId = instructor.StudioId,
            InstructorId = instructor.Id,
            Name = name,
            StartsAt = DateTime.Parse(date + " 09:00", System.Globalization.CultureInfo.InvariantCulture),
            Status = status,
            FixedFee = fixedFee,
            PerBookingPayout = perBooking,
        };

        void Add(BookingStatus s, int n)
        {
            for (var i = 0; i < n; i++)
            {
                cls.Bookings.Add(new Booking { Id = Guid.NewGuid(), ClassId = cls.Id, CustomerName = $"c{i}", Status = s });
            }
        }

        Add(BookingStatus.Completed, attended);
        Add(BookingStatus.NoShow, noShow);
        Add(BookingStatus.Cancelled, cancelled);
        return cls;
    }

    public static Sale Sale(Instructor instructor, SaleType type, decimal amount, string soldAt, string? refundedAt = null) => new()
    {
        Id = Guid.NewGuid(),
        StudioId = instructor.StudioId,
        InstructorId = instructor.Id,
        Type = type,
        Description = type.ToString(),
        Amount = amount,
        SoldAt = DateTime.Parse(soldAt + " 12:00", System.Globalization.CultureInfo.InvariantCulture),
        RefundedAt = refundedAt is null ? null : DateTime.Parse(refundedAt + " 12:00", System.Globalization.CultureInfo.InvariantCulture),
    };

    public static ManualAdjustment Adjustment(Instructor instructor, decimal amount, string effectiveDate, string reason = "test") => new()
    {
        Id = Guid.NewGuid(),
        InstructorId = instructor.Id,
        Amount = amount,
        EffectiveDate = DateOnly.Parse(effectiveDate, System.Globalization.CultureInfo.InvariantCulture),
        Reason = reason,
    };
}
