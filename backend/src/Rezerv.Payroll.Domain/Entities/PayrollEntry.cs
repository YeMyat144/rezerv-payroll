namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// One instructor's payroll within a run. Totals are denormalised from <see cref="LineItems"/>
/// (the audit ledger) so summary queries don't need to aggregate the ledger every time.
/// </summary>
public class PayrollEntry
{
    public Guid Id { get; set; }
    public Guid PayrollRunId { get; set; }
    public PayrollRun PayrollRun { get; set; } = null!;

    public Guid InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    /// <summary>Snapshot of the instructor's name at generation time.</summary>
    public string InstructorName { get; set; } = string.Empty;
    public string StudioName { get; set; } = string.Empty;
    public string Currency { get; set; } = "SGD";
    public NoShowPayoutPolicy NoShowPayoutPolicyApplied { get; set; }

    // ---- Totals -------------------------------------------------------------------------
    public decimal FixedClassEarnings { get; set; }
    public decimal BookingEarnings { get; set; }
    public decimal Commission { get; set; }
    public decimal Bonus { get; set; }
    /// <summary>Refund reversals + manual adjustments. Usually negative.</summary>
    public decimal Adjustment { get; set; }
    public decimal FinalPayout { get; set; }

    // ---- Counts (for the breakdown / audit) -----------------------------------------------
    public int ClassesCompleted { get; set; }
    public int ClassesCancelled { get; set; }
    public int PayableBookings { get; set; }
    public int UnpaidNoShowBookings { get; set; }

    public ICollection<PayrollClassLine> ClassLines { get; set; } = new List<PayrollClassLine>();
    public ICollection<PayrollLineItem> LineItems { get; set; } = new List<PayrollLineItem>();

    /// <summary>Fixed + booking-based earnings; matches the "classEarnings" column in the summary.</summary>
    public decimal ClassEarnings => FixedClassEarnings + BookingEarnings;
}
