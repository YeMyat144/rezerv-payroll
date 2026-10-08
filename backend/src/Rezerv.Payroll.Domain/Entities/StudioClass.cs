namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// A scheduled class taught by one instructor. Compensation rules are captured on the class
/// itself (snapshot of what was agreed at scheduling time) so later changes to a class template
/// do not silently rewrite historical payroll.
/// </summary>
public class StudioClass
{
    public Guid Id { get; set; }
    public Guid StudioId { get; set; }
    public Studio Studio { get; set; } = null!;

    public Guid InstructorId { get; set; }
    public Instructor Instructor { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    /// <summary>Studio-local start time of the class. The date part decides which payroll period it falls into.</summary>
    public DateTime StartsAt { get; set; }

    public int DurationMinutes { get; set; } = 60;

    public ClassStatus Status { get; set; } = ClassStatus.Completed;

    /// <summary>Fixed amount paid to the instructor for completing the class. 0 = no fixed fee.</summary>
    public decimal FixedFee { get; set; }

    /// <summary>Amount paid per payable booking. Null = class has no booking-based compensation.</summary>
    public decimal? PerBookingPayout { get; set; }

    public ICollection<Booking> Bookings { get; set; } = new List<Booking>();

    public DateOnly Date => DateOnly.FromDateTime(StartsAt);
}
