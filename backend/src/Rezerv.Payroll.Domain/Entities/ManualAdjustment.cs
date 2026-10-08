namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// A studio-entered one-off correction to an instructor's payout (positive or negative),
/// e.g. a travel allowance or a clawback for a previous over-payment. Applied to the payroll
/// period containing <see cref="EffectiveDate"/>.
/// </summary>
public class ManualAdjustment
{
    public Guid Id { get; set; }
    public Guid InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    public DateOnly EffectiveDate { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
}
