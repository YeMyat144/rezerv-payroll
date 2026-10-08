using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Application.Payroll;

internal static class PayrollMapper
{
    public static PayrollRunDto ToDto(PayrollRun run) => new(
        run.Id, run.StartDate, run.EndDate, run.GeneratedAtUtc, run.Status.ToString(),
        run.Entries.Count, run.Entries.Sum(e => e.FinalPayout));

    public static PayrollSummaryDto ToSummary(PayrollEntry e) => new(
        e.InstructorId, e.InstructorName, e.StudioName, e.Currency,
        e.FixedClassEarnings, e.BookingEarnings, e.ClassEarnings,
        e.Commission, e.Bonus, e.Adjustment, e.FinalPayout,
        e.ClassesCompleted, e.PayableBookings, e.UnpaidNoShowBookings);

    public static InstructorPayrollDetailDto ToDetail(PayrollEntry e)
    {
        var items = e.LineItems.OrderBy(i => i.OccurredOn).ThenBy(i => i.Type).ToList();

        var commissions = items
            .Where(i => i.Type == PayrollLineItemType.SalesCommission)
            .Select(i => new CommissionRecordDto(i.ReferenceId, i.Description, i.OccurredOn, i.UnitAmount ?? 0, i.Rate ?? 0, i.Amount))
            .ToList();

        var refunds = items
            .Where(i => i.Type == PayrollLineItemType.RefundAdjustment)
            .Select(i => new RefundAdjustmentDto(i.ReferenceId, i.Description, i.OccurredOn, i.UnitAmount ?? 0, i.Rate ?? 0, i.Amount))
            .ToList();

        var bonuses = items
            .Where(i => i.Type == PayrollLineItemType.AttendanceBonus)
            .Select(i => new BonusRecordDto(i.ReferenceId, i.Description, i.OccurredOn, i.Quantity ?? 0, i.Amount))
            .ToList();

        var adjustments = items
            .Where(i => i.Type is PayrollLineItemType.RefundAdjustment or PayrollLineItemType.ManualAdjustment)
            .Select(i => new PayoutAdjustmentDto(i.ReferenceId, i.Type.ToString(), i.Description, i.OccurredOn, i.Amount))
            .ToList();

        var classes = e.ClassLines
            .OrderBy(c => c.StartsAt)
            .Select(c => new ClassBreakdownDto(
                c.ClassId, c.ClassName, c.StartsAt, c.Status.ToString(), c.FixedFee, c.PerBookingPayout,
                c.AttendedBookings, c.NoShowBookings, c.CancelledBookings, c.PayableBookings, c.UnpaidNoShowBookings,
                c.FixedFeeEarned, c.BookingEarnings, c.BonusEarned, c.Total))
            .ToList();

        return new InstructorPayrollDetailDto(
            e.InstructorId, e.InstructorName, e.StudioName, e.Currency, e.NoShowPayoutPolicyApplied.ToString(),
            new PayrollPeriodDto(e.PayrollRun.StartDate, e.PayrollRun.EndDate),
            e.PayrollRun.GeneratedAtUtc,
            new PayrollTotalsDto(e.FixedClassEarnings, e.BookingEarnings, e.ClassEarnings, e.Commission, e.Bonus, e.Adjustment, e.FinalPayout),
            e.ClassesCompleted, e.ClassesCancelled, e.PayableBookings, e.UnpaidNoShowBookings,
            classes, commissions, refunds, bonuses, adjustments,
            items.Select(i => new LineItemDto(i.Id, i.Type.ToString(), i.Description, i.OccurredOn, i.ReferenceType, i.ReferenceId,
                i.Quantity, i.UnitAmount, i.Rate, i.Amount)).ToList());
    }
}
