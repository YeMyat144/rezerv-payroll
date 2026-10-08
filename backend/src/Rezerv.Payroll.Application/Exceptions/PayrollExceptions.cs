using Rezerv.Payroll.Domain;

namespace Rezerv.Payroll.Application.Exceptions;

public abstract class PayrollException(string message) : Exception(message);

/// <summary>Input failed validation (400).</summary>
public sealed class PayrollValidationException(string message) : PayrollException(message);

/// <summary>Requested resource does not exist (404).</summary>
public sealed class PayrollNotFoundException(string message) : PayrollException(message);

/// <summary>
/// A payroll run already exists for an overlapping (but not identical) period (409).
/// Paying an instructor for the same class twice is the worst payroll bug, so we refuse by default.
/// </summary>
public sealed class PayrollPeriodOverlapException(PayrollPeriod requested, IReadOnlyList<PayrollPeriod> conflicts)
    : PayrollException($"Period {requested} overlaps existing payroll run(s): {string.Join(", ", conflicts)}. " +
                       "Generate with the identical period to retrieve the existing run, or regenerate it explicitly.")
{
    public PayrollPeriod Requested { get; } = requested;
    public IReadOnlyList<PayrollPeriod> Conflicts { get; } = conflicts;
}

/// <summary>Two concurrent generate requests raced for the same period; the loser gets this (409).</summary>
public sealed class DuplicatePayrollRunException(PayrollPeriod period)
    : PayrollException($"A payroll run for {period} was created concurrently. Retry the request to fetch it.")
{
    public PayrollPeriod Period { get; } = period;
}
