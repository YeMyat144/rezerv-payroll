namespace Rezerv.Payroll.Domain.Entities;

/// <summary>
/// A studio (tenant). Payroll rules are configured per studio so two studios can
/// have different no-show policies, commission rates and bonus thresholds.
/// </summary>
public class Studio
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Currency { get; set; } = "SGD";

    public NoShowPayoutPolicy NoShowPayoutPolicy { get; set; } = NoShowPayoutPolicy.NoPayout;

    /// <summary>Commission rate on membership sales, e.g. 0.10 = 10%.</summary>
    public decimal MembershipCommissionRate { get; set; } = 0.10m;

    /// <summary>Commission rate on package sales, e.g. 0.05 = 5%.</summary>
    public decimal PackageCommissionRate { get; set; } = 0.05m;

    /// <summary>Attendance strictly greater than this value earns the bonus.</summary>
    public int AttendanceBonusThreshold { get; set; } = 15;

    public decimal AttendanceBonusAmount { get; set; } = 20m;

    public ICollection<Instructor> Instructors { get; set; } = new List<Instructor>();
}
