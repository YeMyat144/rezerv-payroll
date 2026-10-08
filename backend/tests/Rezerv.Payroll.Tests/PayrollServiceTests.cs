using Microsoft.Extensions.Time.Testing;
using Rezerv.Payroll.Application.Abstractions;
using Rezerv.Payroll.Application.Exceptions;
using Rezerv.Payroll.Application.Payroll;
using Rezerv.Payroll.Domain;
using Rezerv.Payroll.Domain.Entities;
using Rezerv.Payroll.Domain.Services;
using static Rezerv.Payroll.Tests.TestData;

namespace Rezerv.Payroll.Tests;

public class PayrollServiceTests
{
    private readonly InMemoryPayrollRepository _repo = new();
    private readonly PayrollService _service;

    public PayrollServiceTests()
    {
        var clock = new FakeTimeProvider(new DateTimeOffset(2026, 7, 1, 9, 0, 0, TimeSpan.Zero));
        _service = new PayrollService(_repo, new PayrollCalculator(), clock);
    }

    private Instructor SeedInstructorWithJuneClass(string name = "John")
    {
        var studio = Studio();
        var instructor = Instructor(studio, name);
        _repo.Instructors.Add(instructor);
        _repo.Classes.Add(Class(instructor, fixedFee: 30, perBooking: null, attended: 5));
        return instructor;
    }

    [Fact]
    public async Task Generate_CreatesRun_AndSummaryReflectsIt()
    {
        SeedInstructorWithJuneClass();

        var result = await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        Assert.False(result.AlreadyExisted);
        Assert.Equal(1, result.Run.InstructorCount);
        Assert.Equal(30m, result.Run.TotalPayout);

        var summary = await _service.GetSummaryAsync(June.StartDate, June.EndDate, CancellationToken.None);
        Assert.Single(summary);
        Assert.Equal(30m, summary[0].FinalPayout);
    }

    [Fact]
    public async Task Generate_SamePeriodTwice_IsIdempotent()
    {
        SeedInstructorWithJuneClass();
        var request = new GeneratePayrollRequest(June.StartDate, June.EndDate);

        var first = await _service.GenerateAsync(request, CancellationToken.None);
        var second = await _service.GenerateAsync(request, CancellationToken.None);

        Assert.True(second.AlreadyExisted);
        Assert.Equal(first.Run.Id, second.Run.Id);
        Assert.Single(_repo.Runs);
    }

    [Fact]
    public async Task Generate_WithRegenerate_ReplacesExistingRun_PickingUpCorrectedData()
    {
        var instructor = SeedInstructorWithJuneClass();
        var first = await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        // Studio corrects the data after the first run: a forgotten class is added.
        _repo.Classes.Add(Class(instructor, date: "2026-06-20", fixedFee: 50, perBooking: null));

        var second = await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate, Regenerate: true), CancellationToken.None);

        Assert.True(second.Regenerated);
        Assert.NotEqual(first.Run.Id, second.Run.Id);
        Assert.Equal(80m, second.Run.TotalPayout);
        Assert.Single(_repo.Runs);
    }

    [Fact]
    public async Task Generate_OverlappingDifferentPeriod_IsRejectedWith409Semantics()
    {
        SeedInstructorWithJuneClass();
        await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        var overlapping = new GeneratePayrollRequest(new DateOnly(2026, 6, 15), new DateOnly(2026, 7, 15));
        var ex = await Assert.ThrowsAsync<PayrollPeriodOverlapException>(() => _service.GenerateAsync(overlapping, CancellationToken.None));

        Assert.Single(ex.Conflicts);
        Assert.Equal(June, ex.Conflicts[0]);
        Assert.Single(_repo.Runs);
    }

    [Fact]
    public async Task Generate_AdjacentPeriods_DoNotOverlap()
    {
        SeedInstructorWithJuneClass();
        await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        var july = await _service.GenerateAsync(new GeneratePayrollRequest(new DateOnly(2026, 7, 1), new DateOnly(2026, 7, 31)), CancellationToken.None);

        Assert.False(july.AlreadyExisted);
        Assert.Equal(2, _repo.Runs.Count);
    }

    [Fact]
    public async Task Generate_EmptyPeriod_ReturnsValidEmptyRun()
    {
        SeedInstructorWithJuneClass();

        var result = await _service.GenerateAsync(new GeneratePayrollRequest(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31)), CancellationToken.None);

        Assert.Equal(0, result.Run.InstructorCount);
        Assert.Empty(result.Summary);
        Assert.Empty(await _service.GetSummaryAsync(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 31), CancellationToken.None));
    }

    [Fact]
    public async Task Generate_InstructorsWithoutActivity_AreOmitted()
    {
        SeedInstructorWithJuneClass("Active");
        _repo.Instructors.Add(Instructor(Studio(), "Idle"));

        var result = await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        Assert.Single(result.Summary);
        Assert.Equal("Active", result.Summary[0].InstructorName);
    }

    [Fact]
    public async Task Generate_RejectsInvalidPeriods()
    {
        await Assert.ThrowsAsync<PayrollValidationException>(() =>
            _service.GenerateAsync(new GeneratePayrollRequest(new DateOnly(2026, 6, 30), new DateOnly(2026, 6, 1)), CancellationToken.None));

        await Assert.ThrowsAsync<PayrollValidationException>(() =>
            _service.GenerateAsync(new GeneratePayrollRequest(new DateOnly(2020, 1, 1), new DateOnly(2026, 1, 1)), CancellationToken.None));
    }

    [Fact]
    public async Task GetSummary_BeforeGeneration_ReturnsEmptyList_NotError()
    {
        var summary = await _service.GetSummaryAsync(June.StartDate, June.EndDate, CancellationToken.None);
        Assert.Empty(summary);
    }

    [Fact]
    public async Task GetDetail_ReturnsFullBreakdown_WithLedgerReconciling()
    {
        var instructor = SeedInstructorWithJuneClass();
        _repo.Sales.Add(Sale(instructor, SaleType.Membership, 200m, "2026-06-05"));
        await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        var detail = await _service.GetInstructorDetailAsync(instructor.Id, June.StartDate, June.EndDate, CancellationToken.None);

        Assert.Equal(50m, detail.Totals.FinalPayout);
        Assert.Single(detail.Classes);
        Assert.Single(detail.Commissions);
        Assert.Equal(detail.Totals.FinalPayout, detail.LineItems.Sum(i => i.Amount));
    }

    [Fact]
    public async Task GetDetail_UnknownPeriodOrInstructor_Throws404Semantics()
    {
        var instructor = SeedInstructorWithJuneClass();

        await Assert.ThrowsAsync<PayrollNotFoundException>(() =>
            _service.GetInstructorDetailAsync(instructor.Id, June.StartDate, June.EndDate, CancellationToken.None));

        await _service.GenerateAsync(new GeneratePayrollRequest(June.StartDate, June.EndDate), CancellationToken.None);

        await Assert.ThrowsAsync<PayrollNotFoundException>(() =>
            _service.GetInstructorDetailAsync(Guid.NewGuid(), June.StartDate, June.EndDate, CancellationToken.None));
    }

    // ------------------------------------------------------------------------------------------

    /// <summary>In-memory IPayrollRepository mirroring the EF implementation's query semantics.</summary>
    private sealed class InMemoryPayrollRepository : IPayrollRepository
    {
        public List<Instructor> Instructors { get; } = [];
        public List<StudioClass> Classes { get; } = [];
        public List<Sale> Sales { get; } = [];
        public List<ManualAdjustment> Adjustments { get; } = [];
        public List<PayrollRun> Runs { get; } = [];

        private readonly List<PayrollRun> _pendingAdds = [];
        private readonly List<PayrollRun> _pendingRemoves = [];

        public Task<IReadOnlyList<Instructor>> GetInstructorsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Instructor>>(Instructors.ToList());

        public Task<IReadOnlyList<StudioClass>> GetClassesInPeriodAsync(PayrollPeriod period, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<StudioClass>>(Classes.Where(c => period.Contains(c.StartsAt)).ToList());

        public Task<IReadOnlyList<Sale>> GetSalesAffectingPeriodAsync(PayrollPeriod period, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<Sale>>(Sales.Where(s => period.Contains(s.SoldAt) || (s.RefundedAt is { } r && period.Contains(r))).ToList());

        public Task<IReadOnlyList<ManualAdjustment>> GetManualAdjustmentsInPeriodAsync(PayrollPeriod period, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<ManualAdjustment>>(Adjustments.Where(a => period.Contains(a.EffectiveDate)).ToList());

        public Task<PayrollRun?> GetRunByPeriodAsync(PayrollPeriod period, bool includeDetails, CancellationToken ct) =>
            Task.FromResult(Runs.SingleOrDefault(r => r.Period == period));

        public Task<IReadOnlyList<PayrollRun>> GetRunsOverlappingAsync(PayrollPeriod period, CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PayrollRun>>(Runs.Where(r => r.Period.Overlaps(period)).ToList());

        public Task<IReadOnlyList<PayrollRun>> ListRunsAsync(CancellationToken ct) =>
            Task.FromResult<IReadOnlyList<PayrollRun>>(Runs.ToList());

        public Task<PayrollEntry?> GetEntryAsync(PayrollPeriod period, Guid instructorId, CancellationToken ct)
        {
            var run = Runs.SingleOrDefault(r => r.Period == period);
            var entry = run?.Entries.SingleOrDefault(e => e.InstructorId == instructorId);
            if (entry is not null)
            {
                entry.PayrollRun = run!;
            }

            return Task.FromResult(entry);
        }

        public Task AddRunAsync(PayrollRun run, CancellationToken ct)
        {
            _pendingAdds.Add(run);
            return Task.CompletedTask;
        }

        public void RemoveRun(PayrollRun run) => _pendingRemoves.Add(run);

        public Task SaveChangesAsync(CancellationToken ct)
        {
            foreach (var r in _pendingRemoves) Runs.Remove(r);
            foreach (var r in _pendingAdds)
            {
                if (Runs.Any(x => x.Period == r.Period)) throw new DuplicatePayrollRunException(r.Period);
                foreach (var e in r.Entries) e.PayrollRun = r;
                Runs.Add(r);
            }

            _pendingRemoves.Clear();
            _pendingAdds.Clear();
            return Task.CompletedTask;
        }
    }
}
