using Microsoft.EntityFrameworkCore;
using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Infrastructure.Persistence;

/// <summary>
/// Deterministic sample data. IDs are fixed so the README can reference them and so re-running
/// against an existing database is a no-op. The reference payroll period is 2026-06-01 → 2026-06-30.
///
/// Expected June 2026 results (class = fixed + booking earnings; also listed in the README):
///   John Tan      class 370.00  commission 55.00  bonus 20.00  adjustment -30.00  final 415.00
///   Sarah Lim     class 256.00  commission 25.00  bonus  0.00  adjustment  +7.50  final 288.50
///   Mike Chen     class 470.00  commission 60.00  bonus 20.00  adjustment  -6.00  final 544.00
///   Aisha Rahman  class 408.00  commission  0.00  bonus 20.00  adjustment -25.00  final 403.00
///   Priya Nair    (no June activity → omitted from the June run)
/// </summary>
public static class DbSeeder
{
    // ---- Stable IDs ----------------------------------------------------------------------
    public static readonly Guid StudioFlowYoga = new("a0000000-0000-0000-0000-000000000001");
    public static readonly Guid StudioPulseHiit = new("a0000000-0000-0000-0000-000000000002");

    public static readonly Guid InstructorJohn = new("10000000-0000-0000-0000-000000000001");
    public static readonly Guid InstructorSarah = new("10000000-0000-0000-0000-000000000002");
    public static readonly Guid InstructorMike = new("10000000-0000-0000-0000-000000000003");
    public static readonly Guid InstructorAisha = new("10000000-0000-0000-0000-000000000004");
    public static readonly Guid InstructorPriya = new("10000000-0000-0000-0000-000000000005");

    private static Guid ClassId(int n) => new($"c0000000-0000-0000-0000-{n:000000000000}");
    private static Guid SaleId(int n) => new($"50000000-0000-0000-0000-{n:000000000000}");
    private static Guid AdjId(int n) => new($"ad000000-0000-0000-0000-{n:000000000000}");

    public static async Task SeedAsync(PayrollDbContext db, CancellationToken ct = default)
    {
        if (await db.Studios.AnyAsync(ct))
        {
            return;
        }

        // ---- Studios: one per no-show policy so both branches are exercised --------------------
        var flowYoga = new Studio
        {
            Id = StudioFlowYoga, Name = "Flow Yoga Studio", Currency = "SGD",
            NoShowPayoutPolicy = NoShowPayoutPolicy.PayInstructor,
        };
        var pulseHiit = new Studio
        {
            Id = StudioPulseHiit, Name = "Pulse HIIT Club", Currency = "SGD",
            NoShowPayoutPolicy = NoShowPayoutPolicy.NoPayout,
        };

        var john = new Instructor { Id = InstructorJohn, StudioId = flowYoga.Id, Name = "John Tan", Email = "john@flowyoga.sg" };
        var sarah = new Instructor { Id = InstructorSarah, StudioId = flowYoga.Id, Name = "Sarah Lim", Email = "sarah@flowyoga.sg" };
        var priya = new Instructor { Id = InstructorPriya, StudioId = flowYoga.Id, Name = "Priya Nair", Email = "priya@flowyoga.sg" };
        var mike = new Instructor { Id = InstructorMike, StudioId = pulseHiit.Id, Name = "Mike Chen", Email = "mike@pulsehiit.sg" };
        var aisha = new Instructor { Id = InstructorAisha, StudioId = pulseHiit.Id, Name = "Aisha Rahman", Email = "aisha@pulsehiit.sg" };

        var classes = new List<StudioClass>
        {
            // ---- John (Flow Yoga, policy = PayInstructor) --------------------------------------
            // Spec example: 10 attended + 2 no-show, pay instructor → 12 × 10 = 120 (+30 fixed)
            Class(1, john, "Yoga Flow", "2026-06-02 09:00", fixedFee: 30, perBooking: 10, attended: 10, noShow: 2, cancelled: 1),
            // 16 attended > 15 → attendance bonus
            Class(2, john, "Yoga Flow", "2026-06-09 09:00", fixedFee: 30, perBooking: 10, attended: 16, noShow: 0, cancelled: 2),
            // Fixed fee only (no booking-based compensation on this class)
            Class(3, john, "Yin Yoga", "2026-06-16 19:00", fixedFee: 30, perBooking: null, attended: 8, noShow: 1, cancelled: 0),
            // Cancelled class → zero payout even though it had bookings and a fixed fee
            Class(4, john, "Yoga Flow", "2026-06-23 09:00", fixedFee: 30, perBooking: 10, attended: 0, noShow: 0, cancelled: 5, status: ClassStatus.Cancelled),
            // Out of period (May) — must NOT appear in June payroll
            Class(5, john, "Yoga Flow", "2026-05-26 09:00", fixedFee: 30, perBooking: 10, attended: 9, noShow: 0, cancelled: 0),

            // ---- Sarah (Flow Yoga, policy = PayInstructor) -------------------------------------
            Class(6, sarah, "Pilates Reformer", "2026-06-03 18:00", fixedFee: 40, perBooking: 8, attended: 6, noShow: 1, cancelled: 0),
            // Exactly 15 attended → NOT above threshold → no bonus (boundary case)
            Class(7, sarah, "Pilates Reformer", "2026-06-17 18:00", fixedFee: 40, perBooking: 8, attended: 15, noShow: 0, cancelled: 1),

            // ---- Mike (Pulse HIIT, policy = NoPayout) ------------------------------------------
            // Spec example: 10 attended + 2 no-show, no payout → 10 × 10 = 100 (+50 fixed)
            Class(8, mike, "HIIT Blast", "2026-06-04 07:00", fixedFee: 50, perBooking: 10, attended: 10, noShow: 2, cancelled: 0),
            // 18 attended → bonus; 3 unpaid no-shows
            Class(9, mike, "HIIT Blast", "2026-06-11 07:00", fixedFee: 50, perBooking: 10, attended: 18, noShow: 3, cancelled: 1),
            // Cancelled class with many bookings → zero
            Class(10, mike, "HIIT Blast", "2026-06-18 07:00", fixedFee: 50, perBooking: 10, attended: 0, noShow: 0, cancelled: 12, status: ClassStatus.Cancelled),
            // Low attendance, many no-shows unpaid
            Class(11, mike, "HIIT Blast", "2026-06-25 07:00", fixedFee: 50, perBooking: 10, attended: 4, noShow: 4, cancelled: 0),
            // Out of period (July) — must NOT appear in June payroll
            Class(12, mike, "HIIT Blast", "2026-07-02 07:00", fixedFee: 50, perBooking: 10, attended: 11, noShow: 0, cancelled: 0),

            // ---- Aisha (Pulse HIIT, policy = NoPayout) — booking-only compensation ------------
            Class(13, aisha, "Spin 45", "2026-06-06 08:00", fixedFee: 0, perBooking: 12, attended: 14, noShow: 2, cancelled: 1),
            Class(14, aisha, "Spin 45", "2026-06-20 08:00", fixedFee: 0, perBooking: 12, attended: 20, noShow: 1, cancelled: 0),

            // ---- Priya — July only, so she has no June entry -----------------------------------
            Class(15, priya, "Hatha Yoga", "2026-07-05 10:00", fixedFee: 30, perBooking: 10, attended: 7, noShow: 0, cancelled: 0),
        };

        var sales = new List<Sale>
        {
            // John: 10% membership, 5% package, and a sale refunded within the same period (nets to 0 but both lines shown)
            Sale(1, john, SaleType.Membership, "Unlimited Monthly Membership", "Grace Wong", 200m, "2026-06-05 11:00"),
            Sale(2, john, SaleType.Package, "10-Class Pack", "Daniel Ong", 100m, "2026-06-10 15:30"),
            Sale(3, john, SaleType.Membership, "Quarterly Membership", "Li Wei", 300m, "2026-06-12 10:00", refundedAt: "2026-06-20 09:15"),

            // Sarah: sold in MAY, refunded in JUNE → May payroll (already paid) earned +7.50;
            //        June payroll shows the -7.50 reversal. This is the "refund after payroll generated" scenario.
            Sale(4, sarah, SaleType.Package, "5-Class Pack", "Nurul Huda", 150m, "2026-05-20 14:00", refundedAt: "2026-06-08 16:45"),
            Sale(5, sarah, SaleType.Membership, "Unlimited Monthly Membership", "Kevin Lee", 250m, "2026-06-25 12:00"),

            // Mike
            Sale(6, mike, SaleType.Membership, "Annual Membership (instalment)", "Rachel Teo", 500m, "2026-06-02 08:30"),
            Sale(7, mike, SaleType.Package, "8-Class HIIT Pack", "Marcus Goh", 80m, "2026-06-14 09:00"),
            Sale(8, mike, SaleType.Package, "12-Class HIIT Pack", "Siti Aminah", 120m, "2026-06-03 18:00", refundedAt: "2026-06-28 10:00"),
        };

        var adjustments = new List<ManualAdjustment>
        {
            new() { Id = AdjId(1), InstructorId = sarah.Id, EffectiveDate = new DateOnly(2026, 6, 17), Amount = 15m, Reason = "Travel allowance — off-site Reformer class" },
            new() { Id = AdjId(2), InstructorId = aisha.Id, EffectiveDate = new DateOnly(2026, 6, 1), Amount = -25m, Reason = "Clawback — May overpayment (duplicate class fee)" },
        };

        db.Studios.AddRange(flowYoga, pulseHiit);
        db.Instructors.AddRange(john, sarah, priya, mike, aisha);
        db.Classes.AddRange(classes);
        db.Sales.AddRange(sales);
        db.ManualAdjustments.AddRange(adjustments);
        await db.SaveChangesAsync(ct);
    }

    // ------------------------------------------------------------------------------------------

    private static StudioClass Class(
        int n, Instructor instructor, string name, string startsAt, decimal fixedFee, decimal? perBooking,
        int attended, int noShow, int cancelled, ClassStatus status = ClassStatus.Completed)
    {
        var cls = new StudioClass
        {
            Id = ClassId(n),
            StudioId = instructor.StudioId,
            InstructorId = instructor.Id,
            Name = name,
            StartsAt = DateTime.Parse(startsAt, System.Globalization.CultureInfo.InvariantCulture),
            DurationMinutes = 60,
            Status = status,
            FixedFee = fixedFee,
            PerBookingPayout = perBooking,
        };

        var seq = 0;
        void Add(BookingStatus s, int count)
        {
            for (var i = 0; i < count; i++)
            {
                seq++;
                cls.Bookings.Add(new Booking
                {
                    Id = new Guid($"b0000000-0000-0000-{n:0000}-{seq:000000000000}"),
                    ClassId = cls.Id,
                    CustomerName = $"Customer {n}-{seq}",
                    Status = s,
                });
            }
        }

        Add(BookingStatus.Completed, attended);
        Add(BookingStatus.NoShow, noShow);
        Add(BookingStatus.Cancelled, cancelled);
        return cls;
    }

    private static Sale Sale(int n, Instructor instructor, SaleType type, string description, string customer, decimal amount, string soldAt, string? refundedAt = null) =>
        new()
        {
            Id = SaleId(n),
            StudioId = instructor.StudioId,
            InstructorId = instructor.Id,
            Type = type,
            Description = description,
            CustomerName = customer,
            Amount = amount,
            SoldAt = DateTime.Parse(soldAt, System.Globalization.CultureInfo.InvariantCulture),
            RefundedAt = refundedAt is null ? null : DateTime.Parse(refundedAt, System.Globalization.CultureInfo.InvariantCulture),
        };
}
