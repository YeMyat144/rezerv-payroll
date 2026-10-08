using System.Globalization;

namespace Rezerv.Payroll.Domain;

/// <summary>
/// An inclusive date range used to scope a payroll run. Immutable value object.
/// </summary>
public readonly record struct PayrollPeriod
{
    public DateOnly StartDate { get; }
    public DateOnly EndDate { get; }

    public PayrollPeriod(DateOnly startDate, DateOnly endDate)
    {
        if (endDate < startDate)
        {
            throw new ArgumentException("endDate must be on or after startDate.", nameof(endDate));
        }

        StartDate = startDate;
        EndDate = endDate;
    }

    public bool Contains(DateOnly date) => date >= StartDate && date <= EndDate;

    public bool Contains(DateTime dateTime) => Contains(DateOnly.FromDateTime(dateTime));

    public bool Overlaps(PayrollPeriod other) => StartDate <= other.EndDate && other.StartDate <= EndDate;

    public int DayCount => EndDate.DayNumber - StartDate.DayNumber + 1;

    // Invariant culture on purpose: on a Thai-locale host the default calendar is Buddhist (year 2569).
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"{StartDate:yyyy-MM-dd} → {EndDate:yyyy-MM-dd}");
}
