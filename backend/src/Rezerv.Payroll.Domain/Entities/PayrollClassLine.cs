namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// Per-class breakdown snapshot for an instructor's payroll entry. Shows how each class
/// contributed (fixed fee, payable bookings, unpaid no-shows, bonus), including cancelled classes
/// with zero payout so the auditor can see they were considered.
/// </summary>
public class PayrollClassLine
{
    public Guid Id { get; set; }
    public Guid PayrollEntryId { get; set; }
    public PayrollEntry PayrollEntry { get; set; } = null!;

    public Guid ClassId { get; set; }
    public string ClassName { get; set; } = string.Empty;
    public DateTime StartsAt { get; set; }
    public ClassStatus Status { get; set; }

    public decimal FixedFee { get; set; }
    public decimal? PerBookingPayout { get; set; }

    public int AttendedBookings { get; set; }
    public int NoShowBookings { get; set; }
    public int CancelledBookings { get; set; }

    /// <summary>Bookings that generated payout (attended + no-shows when policy = PayInstructor).</summary>
    public int PayableBookings { get; set; }
    /// <summary>No-show bookings that generated no payout (policy = NoPayout).</summary>
    public int UnpaidNoShowBookings { get; set; }

    public decimal FixedFeeEarned { get; set; }
    public decimal BookingEarnings { get; set; }
    public decimal BonusEarned { get; set; }

    public decimal Total => FixedFeeEarned + BookingEarnings + BonusEarned;
}
