using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;

namespace Rezerv.Payroll.Application.Abstractions;

/// <summary>
/// Persistence boundary for the payroll use cases. Infrastructure implements this with EF Core;
/// tests can use an in-memory fake. Methods are shaped around what the use cases need rather
/// than being a generic CRUD repository.
/// </summary>
public interface IPayrollRepository
{
    // ---- Source data -------------------------------------------------------------------
    Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct);
    Task<IReadOnlyList<StudioClass>> GetClassesInPeriodAsync(PayrollPeriod period, CancellationToken ct);
    /// <summary>Sales sold in the period OR refunded in the period (refunds may belong to an earlier sale).</summary>
    Task<IReadOnlyList<Sale>> GetSalesAffectingPeriodAsync(PayrollPeriod period, CancellationToken ct);
    Task<IReadOnlyList<ManualAdjustment>> GetManualAdjustmentsInPeriodAsync(PayrollPeriod period, CancellationToken ct);

    // ---- Payroll runs ------------------------------------------------------------------
    Task<PayrollRun?> GetRunByPeriodAsync(PayrollPeriod period, bool includeDetails, CancellationToken ct);
    Task<IReadOnlyList<PayrollRun>> GetRunsOverlappingAsync(PayrollPeriod period, CancellationToken ct);
    Task<IReadOnlyList<PayrollRun>> ListRunsAsync(CancellationToken ct);
    Task<PayrollEntry?> GetEntryAsync(PayrollPeriod period, Guid instructorId, CancellationToken ct);

    Task AddRunAsync(PayrollRun run, CancellationToken ct);
    void RemoveRun(PayrollRun run);

    /// <summary>Persists pending changes. Throws <see cref="Exceptions.DuplicatePayrollRunException"/> if a unique period constraint is violated.</summary>
    Task SaveChangesAsync(CancellationToken ct);
}
