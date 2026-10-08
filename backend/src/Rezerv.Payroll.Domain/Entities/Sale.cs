namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// A membership or package sold by an instructor. Commission is earned in the period of
/// <see cref="SoldAt"/>; if refunded, a reversing adjustment is booked in the period of
/// <see cref="RefundedAt"/>, which may be a later payroll period than the original sale.
/// </summary>
public class Sale
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Studio Studio { get; set; } = null!;

    public Guid InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    public SaleType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;

    public decimal Amount { get; set; }

    public DateTime SoldAt { get; set; }
    public DateTime? RefundedAt { get; set; }

    public bool IsRefunded => RefundedAt.HasValue;
}
