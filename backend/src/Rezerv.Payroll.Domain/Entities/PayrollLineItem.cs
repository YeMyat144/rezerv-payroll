namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// Audit ledger. Every cent in a <see cref="PayrollEntry"/> is explained by the sum of its line items.
/// Informational zero-amount items (unpaid no-shows, cancelled classes) are included on purpose.
/// </summary>
public class PayrollLineItem
{
    public Guid Id { get; set; }
    public Guid PayrollEntryId { get; set; }
    public PayrollEntry PayrollEntry { get; set; } = null!;

    public PayrollLineItemType Type { get; set; }
    public string Description { get; set; } = string.Empty;

    /// <summary>Date of the underlying event (class date, sale date, refund date, adjustment date).</summary>
    public DateOnly OccurredOn { get; set; }

    /// <summary>"Class", "Sale" or "ManualAdjustment" — tells the auditor what <see cref="ReferenceId"/> points at.</summary>
    public string ReferenceType { get; set; } = string.Empty;
    public Guid ReferenceId { get; set; }

    public int? Quantity { get; set; }
    public decimal? UnitAmount { get; set; }
    /// <summary>Commission rate used, when applicable (e.g. 0.10).</summary>
    public decimal? Rate { get; set; }

    public decimal Amount { get; set; }
}
