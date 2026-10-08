using System.Globalization;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Domain.Services;

/// <summary>
/// Pure, side-effect free payroll rules engine. Given an instructor, their studio's payroll
/// configuration, and the raw source records, it produces a fully explained <see cref="PayrollEntry"/>.
///
/// Rules implemented (see README for the assumptions behind each):
///  1. Fixed class fee      – paid once per <b>completed</b> class.
///  2. Booking payout       – per-booking amount × payable bookings. Cancelled bookings never count.
///                            No-shows count only when the studio's policy is <see cref="NoShowPayoutPolicy.PayInstructor"/>.
///  3. Attendance bonus     – paid when attended bookings (excludes cancelled and no-show) exceed the
///                            studio threshold, completed classes only.
///  4. Sales commission     – rate by sale type, earned in the period the sale happened.
///  5. Refund adjustment    – negative reversal in the period the refund happened (may be a later period).
///  6. Manual adjustment    – studio-entered corrections in the period of their effective date.
/// </summary>
public sealed class PayrollCalculator
{
    public PayrollEntry Calculate(
        Instructor instructor,
        Studio studio,
        PayrollPeriod period,
        IEnumerable<StudioClass> classes,
        IEnumerable<Sale> sales,
        IEnumerable<ManualAdjustment> manualAdjustments)
    {
        ArgumentNullException.ThrowIfNull(instructor);
        ArgumentNullException.ThrowIfNull(studio);

        var entry = new PayrollEntry
        {
            Id = Guid.NewGuid(),
            InstructorId = instructor.Id,
            InstructorName = instructor.Name,
            StudioName = studio.Name,
            Currency = studio.Currency,
            NoShowPayoutPolicyApplied = studio.NoShowPayoutPolicy,
        };

        foreach (var cls in classes.Where(c => c.InstructorId == instructor.Id && period.Contains(c.StartsAt))
                                   .OrderBy(c => c.StartsAt))
        {
            ApplyClass(entry, cls, studio);
        }

        foreach (var sale in sales.Where(s => s.InstructorId == instructor.Id).OrderBy(s => s.SoldAt))
        {
            ApplySale(entry, sale, studio, period);
        }

        foreach (var adj in manualAdjustments.Where(a => a.InstructorId == instructor.Id && period.Contains(a.EffectiveDate))
                                             .OrderBy(a => a.EffectiveDate))
        {
            ApplyManualAdjustment(entry, adj);
        }

        entry.FinalPayout = entry.FixedClassEarnings + entry.BookingEarnings + entry.Commission + entry.Bonus + entry.Adjustment;
        return entry;
    }

    /// <summary>True when the entry contains anything worth showing (even zero-amount informational items).</summary>
    public static bool HasActivity(PayrollEntry entry) => entry.LineItems.Count > 0;

    // ------------------------------------------------------------------------------------------

    private static void ApplyClass(PayrollEntry entry, StudioClass cls, Studio studio)
    {
        var attended = cls.Bookings.Count(b => b.Status == BookingStatus.Completed);
        var noShow = cls.Bookings.Count(b => b.Status == BookingStatus.NoShow);
        var cancelled = cls.Bookings.Count(b => b.Status == BookingStatus.Cancelled);

        var line = new PayrollClassLine
        {
            Id = Guid.NewGuid(),
            ClassId = cls.Id,
            ClassName = cls.Name,
            StartsAt = cls.StartsAt,
            Status = cls.Status,
            FixedFee = cls.FixedFee,
            PerBookingPayout = cls.PerBookingPayout,
            AttendedBookings = attended,
            NoShowBookings = noShow,
            CancelledBookings = cancelled,
        };
        entry.ClassLines.Add(line);

        if (cls.Status == ClassStatus.Cancelled)
        {
            entry.ClassesCancelled++;
            AddItem(entry, PayrollLineItemType.CancelledClassNoPayout, cls.Date, "Class", cls.Id,
                $"{cls.Name} — class cancelled, no payout", amount: 0m);
            return;
        }

        entry.ClassesCompleted++;

        // 1. Fixed class fee
        if (cls.FixedFee > 0)
        {
            line.FixedFeeEarned = cls.FixedFee;
            entry.FixedClassEarnings += cls.FixedFee;
            AddItem(entry, PayrollLineItemType.FixedClassFee, cls.Date, "Class", cls.Id,
                $"{cls.Name} — fixed class fee", amount: cls.FixedFee, quantity: 1, unitAmount: cls.FixedFee);
        }

        // 2. Booking-based payout (+ no-show policy)
        if (cls.PerBookingPayout is { } perBooking && perBooking > 0)
        {
            if (attended > 0)
            {
                var amount = attended * perBooking;
                line.PayableBookings += attended;
                line.BookingEarnings += amount;
                entry.BookingEarnings += amount;
                entry.PayableBookings += attended;
                AddItem(entry, PayrollLineItemType.BookingPayout, cls.Date, "Class", cls.Id,
                    Inv($"{cls.Name} — {attended} attended booking(s) × {perBooking:0.##}"), amount, attended, perBooking);
            }

            if (noShow > 0)
            {
                if (studio.NoShowPayoutPolicy == NoShowPayoutPolicy.PayInstructor)
                {
                    var amount = noShow * perBooking;
                    line.PayableBookings += noShow;
                    line.BookingEarnings += amount;
                    entry.BookingEarnings += amount;
                    entry.PayableBookings += noShow;
                    AddItem(entry, PayrollLineItemType.NoShowBookingPayout, cls.Date, "Class", cls.Id,
                        Inv($"{cls.Name} — {noShow} no-show booking(s) × {perBooking:0.##} (policy: pay instructor)"), amount, noShow, perBooking);
                }
                else
                {
                    line.UnpaidNoShowBookings += noShow;
                    entry.UnpaidNoShowBookings += noShow;
                    AddItem(entry, PayrollLineItemType.NoShowBookingUnpaid, cls.Date, "Class", cls.Id,
                        $"{cls.Name} — {noShow} no-show booking(s) not paid (policy: no payout)", 0m, noShow, perBooking);
                }
            }
        }

        // 3. Attendance bonus — attendance = people who actually attended (excludes cancelled and no-show)
        if (attended > studio.AttendanceBonusThreshold && studio.AttendanceBonusAmount > 0)
        {
            line.BonusEarned = studio.AttendanceBonusAmount;
            entry.Bonus += studio.AttendanceBonusAmount;
            AddItem(entry, PayrollLineItemType.AttendanceBonus, cls.Date, "Class", cls.Id,
                $"{cls.Name} — attendance bonus ({attended} attended > {studio.AttendanceBonusThreshold})",
                studio.AttendanceBonusAmount, attended, null);
        }
    }

    private static void ApplySale(PayrollEntry entry, Sale sale, Studio studio, PayrollPeriod period)
    {
        var rate = sale.Type == SaleType.Membership ? studio.MembershipCommissionRate : studio.PackageCommissionRate;
        var commission = Round(sale.Amount * rate);
        var label = sale.Type == SaleType.Membership ? "Membership" : "Package";

        // 4. Commission is earned in the period the sale was made.
        if (period.Contains(sale.SoldAt))
        {
            entry.Commission += commission;
            AddItem(entry, PayrollLineItemType.SalesCommission, DateOnly.FromDateTime(sale.SoldAt), "Sale", sale.Id,
                Inv($"{label}: {sale.Description} — {rate * 100:0.##}% of {sale.Amount:0.##}"), commission, 1, sale.Amount, rate);
        }

        // 5. Refund reverses the commission in the period the refund was issued.
        //    If sale and refund fall in the same period both lines appear and net to zero (auditable).
        //    If the refund happens after the sale's payroll was already generated, the reversal lands here,
        //    in the refund period — no need to reopen a closed payroll.
        if (sale.RefundedAt is { } refundedAt && period.Contains(refundedAt))
        {
            entry.Adjustment -= commission;
            AddItem(entry, PayrollLineItemType.RefundAdjustment, DateOnly.FromDateTime(refundedAt), "Sale", sale.Id,
                Inv($"{label} refund: {sale.Description} — reversal of {rate * 100:0.##}% commission (sold {sale.SoldAt:yyyy-MM-dd})"),
                -commission, 1, sale.Amount, rate);
        }
    }

    private static void ApplyManualAdjustment(PayrollEntry entry, ManualAdjustment adj)
    {
        entry.Adjustment += adj.Amount;
        AddItem(entry, PayrollLineItemType.ManualAdjustment, adj.EffectiveDate, "ManualAdjustment", adj.Id,
            adj.Reason, adj.Amount);
    }

    private static void AddItem(
        PayrollEntry entry,
        PayrollLineItemType type,
        DateOnly occurredOn,
        string referenceType,
        Guid referenceId,
        string description,
        decimal amount,
        int? quantity = null,
        decimal? unitAmount = null,
        decimal? rate = null)
    {
        entry.LineItems.Add(new PayrollLineItem
        {
            Id = Guid.NewGuid(),
            Type = type,
            OccurredOn = occurredOn,
            ReferenceType = referenceType,
            ReferenceId = referenceId,
            Description = description,
            Amount = amount,
            Quantity = quantity,
            UnitAmount = unitAmount,
            Rate = rate,
        });
    }

    private static decimal Round(decimal value) => Math.Round(value, 2, MidpointRounding.ToEven);

    /// <summary>Culture-invariant formatting so ledger text is identical on every host (e.g. no Buddhist-era years on Thai locales).</summary>
    private static string Inv(FormattableString s) => s.ToString(CultureInfo.InvariantCulture);
}
