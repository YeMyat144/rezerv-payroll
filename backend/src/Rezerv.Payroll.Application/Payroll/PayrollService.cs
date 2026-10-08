using Rezerv.Payroll.Application.Abstractions;
using Rezerv.Payroll.Application.Exceptions;
using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;
using Rezerv.Payroll.Domain.Services;

namespace Rezerv.Payroll.Application.Payroll;

/// <summary>
/// Application use cases for payroll. Orchestrates loading source data, running the domain
/// calculator and persisting the snapshot. Contains no business arithmetic — that lives in
/// <see cref="PayrollCalculator"/>.
/// </summary>
public sealed class PayrollService(IPayrollRepository repository, PayrollCalculator calculator, TimeProvider clock)
{
    /// <summary>Guardrail: payroll periods longer than this are almost certainly a typo.</summary>
    public const int MaxPeriodDays = 366;

    public async Task<GeneratePayrollResponse> GenerateAsync(GeneratePayrollRequest request, CancellationToken ct)
    {
        var period = ParsePeriod(request.StartDate, request.EndDate);

        // 1. Idempotency: identical period → return existing run unchanged (unless explicitly regenerating).
        var existing = await repository.GetRunByPeriodAsync(period, includeDetails: false, ct);
        if (existing is not null && !request.Regenerate)
        {
            return new GeneratePayrollResponse(
                PayrollMapper.ToDto(existing), AlreadyExisted: true, Regenerated: false,
                existing.Entries.OrderBy(e => e.InstructorName).Select(PayrollMapper.ToSummary).ToList());
        }

        // 2. Safety: refuse overlapping-but-different periods — they would pay the same classes twice.
        var overlapping = (await repository.GetRunsOverlappingAsync(period, ct))
            .Where(r => existing is null || r.Id != existing.Id)
            .ToList();
        if (overlapping.Count > 0)
        {
            throw new PayrollPeriodOverlapException(period, overlapping.Select(r => r.Period).ToList());
        }

        // 3. Load everything the calculator needs in a handful of queries (not per instructor).
        var instructors = await repository.GetInstructorsAsync(ct);
        var classes = await repository.GetClassesInPeriodAsync(period, ct);
        var sales = await repository.GetSalesAffectingPeriodAsync(period, ct);
        var adjustments = await repository.GetManualAdjustmentsInPeriodAsync(period, ct);

        var run = new PayrollRun
        {
            Id = Guid.NewGuid(),
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            GeneratedAtUtc = clock.GetUtcNow().UtcDateTime,
            Status = PayrollRunStatus.Generated,
        };

        foreach (var instructor in instructors)
        {
            var entry = calculator.Calculate(instructor, instructor.Studio, period, classes, sales, adjustments);
            // Empty period → no entries → valid empty result. Instructors with zero activity are omitted.
            if (!PayrollCalculator.HasActivity(entry))
            {
                continue;
            }

            entry.PayrollRunId = run.Id;
            run.Entries.Add(entry);
        }

        if (existing is not null)
        {
            repository.RemoveRun(existing);
        }

        await repository.AddRunAsync(run, ct);
        await repository.SaveChangesAsync(ct);

        return new GeneratePayrollResponse(
            PayrollMapper.ToDto(run), AlreadyExisted: false, Regenerated: existing is not null,
            run.Entries.OrderBy(e => e.InstructorName).Select(PayrollMapper.ToSummary).ToList());
    }

    /// <summary>Summary rows for a previously generated period. Returns an empty list if nothing was generated.</summary>
    public async Task<IReadOnlyList<PayrollSummaryDto>> GetSummaryAsync(DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var period = ParsePeriod(startDate, endDate);
        var run = await repository.GetRunByPeriodAsync(period, includeDetails: false, ct);
        return run is null
            ? []
            : run.Entries.OrderBy(e => e.InstructorName).Select(PayrollMapper.ToSummary).ToList();
    }

    public async Task<InstructorPayrollDetailDto> GetInstructorDetailAsync(Guid instructorId, DateOnly startDate, DateOnly endDate, CancellationToken ct)
    {
        var period = ParsePeriod(startDate, endDate);
        var run = await repository.GetRunByPeriodAsync(period, includeDetails: false, ct)
                  ?? throw new PayrollNotFoundException($"No payroll has been generated for {period}. Call POST /api/payroll/generate first.");

        var entry = await repository.GetEntryAsync(period, instructorId, ct)
                    ?? throw new PayrollNotFoundException($"Instructor {instructorId} has no payroll entry for {period} (no activity in this period, or unknown instructor).");

        _ = run; // run existence checked above for a clearer 404 message
        return PayrollMapper.ToDetail(entry);
    }

    public async Task<IReadOnlyList<PayrollRunDto>> ListRunsAsync(CancellationToken ct)
    {
        var runs = await repository.ListRunsAsync(ct);
        return runs.OrderByDescending(r => r.StartDate).Select(PayrollMapper.ToDto).ToList();
    }

    // ------------------------------------------------------------------------------------------

    private static PayrollPeriod ParsePeriod(DateOnly start, DateOnly end)
    {
        if (end < start)
        {
            throw new PayrollValidationException("endDate must be on or after startDate.");
        }

        var period = new PayrollPeriod(start, end);
        if (period.DayCount > MaxPeriodDays)
        {
            throw new PayrollValidationException($"Payroll period cannot exceed {MaxPeriodDays} days.");
        }

        return period;
    }
}
