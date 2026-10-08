using Rezerv.Payroll.Domain;

namespace Rezerv.Payroll.Application.Payroll;

// ---------------------------------------------------------------------------------------------
// Requests
// ---------------------------------------------------------------------------------------------

/// <param name="StartDate">Inclusive, yyyy-MM-dd.</param>
/// <param name="EndDate">Inclusive, yyyy-MM-dd.</param>
/// <param name="Regenerate">
/// When true and a run already exists for the identical period, it is replaced with a fresh
/// calculation (e.g. after correcting bookings). Defaults to false = idempotent.
/// </param>
public sealed record GeneratePayrollRequest(DateOnly StartDate, DateOnly EndDate, bool Regenerate = false);

// ---------------------------------------------------------------------------------------------
// Responses
// ---------------------------------------------------------------------------------------------

public sealed record PayrollRunDto(
    Guid Id,
    DateOnly StartDate,
    DateOnly EndDate,
    DateTime GeneratedAtUtc,
    string Status,
    int InstructorCount,
    decimal TotalPayout);

public sealed record GeneratePayrollResponse(
    PayrollRunDto Run,
    /// <summary>True when an identical period had already been generated and that run was returned unchanged.</summary>
    bool AlreadyExisted,
    /// <summary>True when an existing run for the same period was replaced because Regenerate = true.</summary>
    bool Regenerated,
    IReadOnlyList<PayrollSummaryDto> Summary);

/// <summary>Row in the dashboard summary table. Field names follow the assessment's sample response.</summary>
public sealed record PayrollSummaryDto(
    Guid InstructorId,
    string InstructorName,
    string StudioName,
    string Currency,
    decimal FixedClassEarnings,
    decimal BookingEarnings,
    /// <summary>FixedClassEarnings + BookingEarnings.</summary>
    decimal ClassEarnings,
    decimal Commission,
    decimal Bonus,
    decimal Adjustment,
    decimal FinalPayout,
    int ClassesCompleted,
    int PayableBookings,
    int UnpaidNoShowBookings);

public sealed record PayrollPeriodDto(DateOnly StartDate, DateOnly EndDate)
{
    public static PayrollPeriodDto From(PayrollPeriod p) => new(p.StartDate, p.EndDate);
}

public sealed record PayrollTotalsDto(
    decimal FixedClassEarnings,
    decimal BookingEarnings,
    decimal ClassEarnings,
    decimal Commission,
    decimal Bonus,
    decimal Adjustment,
    decimal FinalPayout);

public sealed record ClassBreakdownDto(
    Guid ClassId,
    string ClassName,
    DateTime StartsAt,
    string Status,
    decimal FixedFee,
    decimal? PerBookingPayout,
    int AttendedBookings,
    int NoShowBookings,
    int CancelledBookings,
    int PayableBookings,
    int UnpaidNoShowBookings,
    decimal FixedFeeEarned,
    decimal BookingEarnings,
    decimal BonusEarned,
    decimal Total);

public sealed record CommissionRecordDto(
    Guid SaleId,
    string Description,
    DateOnly SoldOn,
    decimal SaleAmount,
    decimal Rate,
    decimal Commission);

public sealed record RefundAdjustmentDto(
    Guid SaleId,
    string Description,
    DateOnly RefundedOn,
    decimal SaleAmount,
    decimal Rate,
    decimal Amount);

public sealed record BonusRecordDto(
    Guid ClassId,
    string Description,
    DateOnly ClassDate,
    int Attendance,
    decimal Amount);

public sealed record PayoutAdjustmentDto(
    Guid ReferenceId,
    string Type,
    string Description,
    DateOnly EffectiveDate,
    decimal Amount);

public sealed record LineItemDto(
    Guid Id,
    string Type,
    string Description,
    DateOnly OccurredOn,
    string ReferenceType,
    Guid ReferenceId,
    int? Quantity,
    decimal? UnitAmount,
    decimal? Rate,
    decimal Amount);

public sealed record InstructorPayrollDetailDto(
    Guid InstructorId,
    string InstructorName,
    string StudioName,
    string Currency,
    string NoShowPayoutPolicy,
    PayrollPeriodDto Period,
    DateTime GeneratedAtUtc,
    PayrollTotalsDto Totals,
    int ClassesCompleted,
    int ClassesCancelled,
    int PayableBookings,
    int UnpaidNoShowBookings,
    IReadOnlyList<ClassBreakdownDto> Classes,
    IReadOnlyList<CommissionRecordDto> Commissions,
    IReadOnlyList<RefundAdjustmentDto> RefundAdjustments,
    IReadOnlyList<BonusRecordDto> Bonuses,
    /// <summary>All adjustments (refund reversals + manual), i.e. everything that makes up Totals.Adjustment.</summary>
    IReadOnlyList<PayoutAdjustmentDto> PayoutAdjustments,
    /// <summary>Raw audit ledger; sums to Totals.FinalPayout.</summary>
    IReadOnlyList<LineItemDto> LineItems);
