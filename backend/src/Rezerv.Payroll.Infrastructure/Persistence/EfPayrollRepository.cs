using Microsoft.EntityFrameworkCore;
using Rezerv.Payroll.Application.Abstractions;
using Rezerv.Payroll.Application.Exceptions;
using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Infrastructure.Persistence;

public sealed class EfPayrollRepository(PayrollDbContext db) : IPayrollRepository
{
    private static DateTime StartOf(PayrollPeriod p) => p.StartDate.ToDateTime(TimeOnly.MinValue);
    private static DateTime EndExclusive(PayrollPeriod p) => p.EndDate.AddDays(1).ToDateTime(TimeOnly.MinValue);

    public async Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct) =>
        await db.Instructors.AsNoTracking().Include(i => i.Studio).OrderBy(i => i.Name).ToListAsync(ct);

    public async Task<IReadOnlyList<StudioClass>> GetClassesInPeriodAsync(PayrollPeriod period, CancellationToken ct)
    {
        var from = StartOf(period);
        var to = EndExclusive(period);
        return await db.Classes.AsNoTracking()
            .Include(c => c.Bookings)
            .Where(c => c.StartsAt >= from && c.StartsAt < to)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Sale>> GetSalesAffectingPeriodAsync(PayrollPeriod period, CancellationToken ct)
    {
        var from = StartOf(period);
        var to = EndExclusive(period);
        return await db.Sales.AsNoTracking()
            .Where(s => (s.SoldAt >= from && s.SoldAt < to) ||
                        (s.RefundedAt != null && s.RefundedAt >= from && s.RefundedAt < to))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ManualAdjustment>> GetManualAdjustmentsInPeriodAsync(PayrollPeriod period, CancellationToken ct) =>
        await db.ManualAdjustments.AsNoTracking()
            .Where(a => a.EffectiveDate >= period.StartDate && a.EffectiveDate <= period.EndDate)
            .ToListAsync(ct);

    public async Task<PayrollRun?> GetRunByPeriodAsync(PayrollPeriod period, bool includeDetails, CancellationToken ct)
    {
        IQueryable<PayrollRun> query = db.PayrollRuns.Include(r => r.Entries);
        if (includeDetails)
        {
            query = query.Include(r => r.Entries).ThenInclude(e => e.ClassLines)
                         .Include(r => r.Entries).ThenInclude(e => e.LineItems);
        }

        return await query.SingleOrDefaultAsync(r => r.StartDate == period.StartDate && r.EndDate == period.EndDate, ct);
    }

    public async Task<IReadOnlyList<PayrollRun>> GetRunsOverlappingAsync(PayrollPeriod period, CancellationToken ct) =>
        await db.PayrollRuns.AsNoTracking()
            .Where(r => r.StartDate <= period.EndDate && period.StartDate <= r.EndDate)
            .ToListAsync(ct);

    public async Task<IReadOnlyList<PayrollRun>> ListRunsAsync(CancellationToken ct) =>
        await db.PayrollRuns.AsNoTracking().Include(r => r.Entries).ToListAsync(ct);

    public async Task<PayrollEntry?> GetEntryAsync(PayrollPeriod period, Guid instructorId, CancellationToken ct) =>
        await db.PayrollEntries.AsNoTracking()
            .Include(e => e.PayrollRun)
            .Include(e => e.ClassLines)
            .Include(e => e.LineItems)
            .SingleOrDefaultAsync(e => e.InstructorId == instructorId &&
                                       e.PayrollRun.StartDate == period.StartDate &&
                                       e.PayrollRun.EndDate == period.EndDate, ct);

    public async Task AddRunAsync(PayrollRun run, CancellationToken ct) => await db.PayrollRuns.AddAsync(run, ct);

    public void RemoveRun(PayrollRun run) => db.PayrollRuns.Remove(run);

    public async Task SaveChangesAsync(CancellationToken ct)
    {
        try
        {
            await db.SaveChangesAsync(ct);
        }
        catch (DbUpdateException ex) when (IsUniqueViolation(ex))
        {
            var run = db.ChangeTracker.Entries<PayrollRun>().Select(e => e.Entity).FirstOrDefault();
            throw new DuplicatePayrollRunException(run?.Period ?? default);
        }
    }

    // MySQL error 1062 = ER_DUP_ENTRY. Checked by message to avoid a hard dependency on the provider's exception type here.
    private static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException?.Message.Contains("Duplicate entry", StringComparison.OrdinalIgnoreCase) == true;
}
