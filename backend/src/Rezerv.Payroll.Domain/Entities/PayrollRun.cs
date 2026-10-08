namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// An immutable snapshot of payroll for one period. Generating payroll persists this run so the
/// numbers an admin saw (and paid) never change even if source bookings/sales are edited later.
/// Uniqueness on (StartDate, EndDate) makes duplicate generation requests idempotent.
/// </summary>
public class PayrollRun
{
    public Guid Id { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public DateTime GeneratedAtUtc { get; set; }
    public PayrollRunStatus Status { get; set; } = PayrollRunStatus.Generated;

    public ICollection<PayrollEntry> Entries { get; set; } = new List<PayrollEntry>();

    public PayrollPeriod Period => new(StartDate, EndDate);

    public decimal TotalPayout => Entries.Sum(e => e.FinalPayout);
}
