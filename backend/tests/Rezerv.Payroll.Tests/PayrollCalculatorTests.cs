using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;
using Rezerv.Payroll.Domain.Services;
using static Rezerv.Payroll.Tests.TestData;

namespace Rezerv.Payroll.Tests;

public class PayrollCalculatorTests
{
    private readonly PayrollCalculator _calc = new();

    private PayrollEntry Run(Instructor instructor, PayrollPeriod period,
        IEnumerable<StudioClass>? classes = null, IEnumerable<Sale>? sales = null, IEnumerable<ManualAdjustment>? adjustments = null)
        => _calc.Calculate(instructor, instructor.Studio, period, classes ?? [], sales ?? [], adjustments ?? []);

    // ------------------------------------------------------------------------------------------
    // 1. Fixed class fee
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void FixedFee_IsPaidOncePerCompletedClass()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var classes = new[]
        {
            Class(john, fixedFee: 30, perBooking: null, name: "Yoga"),
            Class(john, fixedFee: 50, perBooking: null, name: "HIIT", date: "2026-06-12"),
        };

        var entry = Run(john, June, classes);

        Assert.Equal(80m, entry.FixedClassEarnings);
        Assert.Equal(80m, entry.FinalPayout);
        Assert.Equal(2, entry.ClassesCompleted);
    }

    [Fact]
    public void CancelledClass_GeneratesNoPayout_ButIsRecordedForAudit()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var cancelled = Class(john, fixedFee: 30, perBooking: 10, attended: 0, cancelled: 12, status: ClassStatus.Cancelled);

        var entry = Run(john, June, [cancelled]);

        Assert.Equal(0m, entry.FinalPayout);
        Assert.Equal(1, entry.ClassesCancelled);
        Assert.Equal(0, entry.ClassesCompleted);
        Assert.Single(entry.ClassLines);
        Assert.Contains(entry.LineItems, i => i.Type == PayrollLineItemType.CancelledClassNoPayout && i.Amount == 0m);
    }

    // ------------------------------------------------------------------------------------------
    // 2. Booking-based payout + 3. No-show policy
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void BookingPayout_PaysPerAttendedBooking()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: 10, attended: 10);

        var entry = Run(john, June, [cls]);

        Assert.Equal(100m, entry.BookingEarnings);
        Assert.Equal(10, entry.PayableBookings);
    }

    [Fact]
    public void NoShowPolicy_PayInstructor_PaysNoShows_SpecExample()
    {
        // Spec: 10 attended + 2 no-show, Pay Instructor → 12 × 10 = 120
        var studio = Studio(NoShowPayoutPolicy.PayInstructor);
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: 10, attended: 10, noShow: 2);

        var entry = Run(john, June, [cls]);

        Assert.Equal(120m, entry.BookingEarnings);
        Assert.Equal(12, entry.PayableBookings);
        Assert.Equal(0, entry.UnpaidNoShowBookings);
        Assert.Contains(entry.LineItems, i => i.Type == PayrollLineItemType.NoShowBookingPayout && i.Amount == 20m);
    }

    [Fact]
    public void NoShowPolicy_NoPayout_ExcludesNoShows_SpecExample()
    {
        // Spec: 10 attended + 2 no-show, No Payout → 10 × 10 = 100
        var studio = Studio(NoShowPayoutPolicy.NoPayout);
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: 10, attended: 10, noShow: 2);

        var entry = Run(john, June, [cls]);

        Assert.Equal(100m, entry.BookingEarnings);
        Assert.Equal(10, entry.PayableBookings);
        Assert.Equal(2, entry.UnpaidNoShowBookings);
        // Unpaid no-shows are still shown in the breakdown as a zero-amount line.
        var unpaid = Assert.Single(entry.LineItems, i => i.Type == PayrollLineItemType.NoShowBookingUnpaid);
        Assert.Equal(0m, unpaid.Amount);
        Assert.Equal(2, unpaid.Quantity);
    }

    [Theory]
    [InlineData(NoShowPayoutPolicy.PayInstructor)]
    [InlineData(NoShowPayoutPolicy.NoPayout)]
    public void CancelledBookings_NeverCountTowardPayout(NoShowPayoutPolicy policy)
    {
        var studio = Studio(policy);
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: 10, attended: 5, cancelled: 7);

        var entry = Run(john, June, [cls]);

        Assert.Equal(50m, entry.BookingEarnings);
        Assert.Equal(5, entry.PayableBookings);
        Assert.Equal(7, entry.ClassLines.Single().CancelledBookings);
    }

    [Fact]
    public void ClassWithoutPerBookingPayout_HasNoBookingEarnings()
    {
        var studio = Studio(NoShowPayoutPolicy.PayInstructor);
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 30, perBooking: null, attended: 12, noShow: 3);

        var entry = Run(john, June, [cls]);

        Assert.Equal(30m, entry.FixedClassEarnings);
        Assert.Equal(0m, entry.BookingEarnings);
        Assert.DoesNotContain(entry.LineItems, i => i.Type is PayrollLineItemType.BookingPayout or PayrollLineItemType.NoShowBookingPayout);
    }

    // ------------------------------------------------------------------------------------------
    // 5. Attendance bonus
    // ------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(14, 0)]
    [InlineData(15, 0)]   // "exceeds 15" → exactly 15 does NOT qualify
    [InlineData(16, 20)]
    [InlineData(30, 20)]
    public void AttendanceBonus_AppliesOnlyAboveThreshold(int attended, decimal expectedBonus)
    {
        var studio = Studio();
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: null, attended: attended);

        var entry = Run(john, June, [cls]);

        Assert.Equal(expectedBonus, entry.Bonus);
    }

    [Fact]
    public void AttendanceBonus_ExcludesCancelledAndNoShowBookingsFromCount()
    {
        // 14 attended + 5 no-show + 3 cancelled = 22 bookings, but only 14 attended → no bonus
        var studio = Studio(NoShowPayoutPolicy.PayInstructor);
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 0, perBooking: 10, attended: 14, noShow: 5, cancelled: 3);

        var entry = Run(john, June, [cls]);

        Assert.Equal(0m, entry.Bonus);
    }

    [Fact]
    public void AttendanceBonus_NotPaidForCancelledClass()
    {
        var studio = Studio();
        var john = Instructor(studio);
        // Bookings were all "Completed" before the class got cancelled — still nothing is payable.
        var cls = Class(john, attended: 20, status: ClassStatus.Cancelled);

        var entry = Run(john, June, [cls]);

        Assert.Equal(0m, entry.Bonus);
        Assert.Equal(0m, entry.FinalPayout);
    }

    [Fact]
    public void AttendanceBonus_AppearsAsSeparateLineItem_AndIsNotInClassEarnings()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var cls = Class(john, fixedFee: 30, perBooking: 10, attended: 16);

        var entry = Run(john, June, [cls]);

        Assert.Equal(30m + 160m, entry.ClassEarnings);
        Assert.Equal(20m, entry.Bonus);
        Assert.Equal(210m, entry.FinalPayout);
        Assert.Single(entry.LineItems, i => i.Type == PayrollLineItemType.AttendanceBonus);
    }

    // ------------------------------------------------------------------------------------------
    // 4. Sales commission + refunds
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void Commission_Membership10Percent_Package5Percent_SpecExamples()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var sales = new[]
        {
            Sale(john, SaleType.Membership, 200m, "2026-06-05"),
            Sale(john, SaleType.Package, 100m, "2026-06-10"),
        };

        var entry = Run(john, June, sales: sales);

        Assert.Equal(25m, entry.Commission); // 20 + 5
        Assert.Equal(25m, entry.FinalPayout);
    }

    [Fact]
    public void Refund_InSamePeriod_ShowsBothCommissionAndReversal_NettingToZero()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var sale = Sale(john, SaleType.Membership, 200m, "2026-06-05", refundedAt: "2026-06-20");

        var entry = Run(john, June, sales: [sale]);

        Assert.Equal(20m, entry.Commission);
        Assert.Equal(-20m, entry.Adjustment);
        Assert.Equal(0m, entry.FinalPayout);
        Assert.Single(entry.LineItems, i => i.Type == PayrollLineItemType.SalesCommission);
        Assert.Single(entry.LineItems, i => i.Type == PayrollLineItemType.RefundAdjustment && i.Amount == -20m);
    }

    [Fact]
    public void Refund_AfterPayrollPeriodClosed_LandsInRefundPeriod_NotSalePeriod()
    {
        // Sold in May (already paid out), refunded in June → June carries the -commission adjustment.
        var studio = Studio();
        var john = Instructor(studio);
        var sale = Sale(john, SaleType.Package, 150m, "2026-05-20", refundedAt: "2026-06-08");

        var may = Run(john, May, sales: [sale]);
        var june = Run(john, June, sales: [sale]);

        Assert.Equal(7.50m, may.Commission);
        Assert.Equal(0m, may.Adjustment);

        Assert.Equal(0m, june.Commission);
        Assert.Equal(-7.50m, june.Adjustment);
        Assert.Equal(-7.50m, june.FinalPayout); // negative payout = clawback carried into this period
    }

    [Fact]
    public void Commission_IsRoundedToCents()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var sale = Sale(john, SaleType.Package, 33.33m, "2026-06-05"); // 5% = 1.6665

        var entry = Run(john, June, sales: [sale]);

        Assert.Equal(1.67m, entry.Commission);
    }

    // ------------------------------------------------------------------------------------------
    // Manual adjustments
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void ManualAdjustments_AreAppliedInTheirEffectivePeriod_AndCanBePositiveOrNegative()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var adjustments = new[]
        {
            Adjustment(john, 15m, "2026-06-17", "Travel allowance"),
            Adjustment(john, -25m, "2026-06-01", "Clawback"),
            Adjustment(john, 999m, "2026-07-01", "Next month — must be ignored"),
        };

        var entry = Run(john, June, adjustments: adjustments);

        Assert.Equal(-10m, entry.Adjustment);
        Assert.Equal(2, entry.LineItems.Count(i => i.Type == PayrollLineItemType.ManualAdjustment));
    }

    // ------------------------------------------------------------------------------------------
    // Period scoping & isolation
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void Period_IsInclusiveOnBothEnds_AndExcludesOutsideDates()
    {
        var studio = Studio();
        var john = Instructor(studio);
        var classes = new[]
        {
            Class(john, date: "2026-05-31", fixedFee: 1, perBooking: null), // day before
            Class(john, date: "2026-06-01", fixedFee: 10, perBooking: null), // first day
            Class(john, date: "2026-06-30", fixedFee: 100, perBooking: null), // last day
            Class(john, date: "2026-07-01", fixedFee: 1000, perBooking: null), // day after
        };

        var entry = Run(john, June, classes);

        Assert.Equal(110m, entry.FixedClassEarnings);
    }

    [Fact]
    public void OnlyTheTargetInstructorsRecordsAreUsed()
    {
        var studio = Studio();
        var john = Instructor(studio, "John");
        var jane = Instructor(studio, "Jane");
        var classes = new[] { Class(john, fixedFee: 30, perBooking: null), Class(jane, fixedFee: 50, perBooking: null) };
        var sales = new[] { Sale(john, SaleType.Membership, 100m, "2026-06-02"), Sale(jane, SaleType.Membership, 100m, "2026-06-02") };

        var entry = Run(john, June, classes, sales);

        Assert.Equal(30m, entry.FixedClassEarnings);
        Assert.Equal(10m, entry.Commission);
        Assert.Equal(40m, entry.FinalPayout);
    }

    [Fact]
    public void EmptyPeriod_ReturnsZeroEntryWithNoActivity()
    {
        var studio = Studio();
        var john = Instructor(studio);

        var entry = Run(john, June);

        Assert.Equal(0m, entry.FinalPayout);
        Assert.Empty(entry.LineItems);
        Assert.False(PayrollCalculator.HasActivity(entry));
    }

    // ------------------------------------------------------------------------------------------
    // Multiple earning sources together + ledger integrity
    // ------------------------------------------------------------------------------------------

    [Fact]
    public void MultipleEarningSources_CombineCorrectly_AndLedgerSumsToFinalPayout()
    {
        var studio = Studio(NoShowPayoutPolicy.PayInstructor);
        var john = Instructor(studio);
        var classes = new[]
        {
            Class(john, date: "2026-06-02", fixedFee: 30, perBooking: 10, attended: 10, noShow: 2, cancelled: 1), // 30 + 120
            Class(john, date: "2026-06-09", fixedFee: 30, perBooking: 10, attended: 16, cancelled: 2),            // 30 + 160 + bonus 20
            Class(john, date: "2026-06-16", fixedFee: 30, perBooking: null, attended: 8, noShow: 1),             // 30
            Class(john, date: "2026-06-23", fixedFee: 30, perBooking: 10, cancelled: 5, status: ClassStatus.Cancelled), // 0
            Class(john, date: "2026-05-26", fixedFee: 30, perBooking: 10, attended: 9),                           // out of period
        };
        var sales = new[]
        {
            Sale(john, SaleType.Membership, 200m, "2026-06-05"),                               // +20
            Sale(john, SaleType.Package, 100m, "2026-06-10"),                                  // +5
            Sale(john, SaleType.Membership, 300m, "2026-06-12", refundedAt: "2026-06-20"),     // +30 / -30
        };

        var entry = Run(john, June, classes, sales);

        Assert.Equal(90m, entry.FixedClassEarnings);
        Assert.Equal(280m, entry.BookingEarnings);
        Assert.Equal(370m, entry.ClassEarnings);
        Assert.Equal(55m, entry.Commission);
        Assert.Equal(20m, entry.Bonus);
        Assert.Equal(-30m, entry.Adjustment);
        Assert.Equal(415m, entry.FinalPayout);
        Assert.Equal(3, entry.ClassesCompleted);
        Assert.Equal(1, entry.ClassesCancelled);

        // Every cent is explained by the ledger.
        Assert.Equal(entry.FinalPayout, entry.LineItems.Sum(i => i.Amount));
        // And the per-class view agrees with the totals.
        Assert.Equal(entry.FixedClassEarnings, entry.ClassLines.Sum(c => c.FixedFeeEarned));
        Assert.Equal(entry.BookingEarnings, entry.ClassLines.Sum(c => c.BookingEarnings));
        Assert.Equal(entry.Bonus, entry.ClassLines.Sum(c => c.BonusEarned));
    }

    [Fact]
    public void Entry_SnapshotsStudioPolicyAndNames()
    {
        var studio = Studio(NoShowPayoutPolicy.PayInstructor);
        studio.Name = "Flow Yoga";
        var john = Instructor(studio, "John Tan");

        var entry = Run(john, June, [Class(john)]);

        Assert.Equal("John Tan", entry.InstructorName);
        Assert.Equal("Flow Yoga", entry.StudioName);
        Assert.Equal("SGD", entry.Currency);
        Assert.Equal(NoShowPayoutPolicy.PayInstructor, entry.NoShowPayoutPolicyApplied);
    }
}
