using System.ComponentModel.DataAnnotations;
using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Rezerv.Payroll.Application.Payroll;

namespace Rezerv.Payroll.Api.Controllers;

[ApiController]
[Route("api/payroll")]
[Produces("application/json")]
public sealed class PayrollController(PayrollService payroll) : ControllerBase
{
    /// <summary>Generate payroll for all instructors for a period.</summary>
    /// <remarks>
    /// Idempotent: calling again with the identical period returns the existing run (<c>alreadyExisted = true</c>)
    /// without recalculating. Pass <c>"regenerate": true</c> to replace it. A period that overlaps a different
    /// existing run is rejected with 409 to prevent paying the same classes twice.
    /// </remarks>
    [HttpPost("generate")]
    [ProducesResponseType<GeneratePayrollResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<GeneratePayrollResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<GeneratePayrollResponse>> Generate([FromBody] GeneratePayrollRequest request, CancellationToken ct)
    {
        var result = await payroll.GenerateAsync(request, ct);
        if (result.AlreadyExisted)
        {
            return Ok(result);
        }

        var location = string.Create(CultureInfo.InvariantCulture,
            $"/api/payroll?startDate={result.Run.StartDate:yyyy-MM-dd}&endDate={result.Run.EndDate:yyyy-MM-dd}");
        return Created(location, result);
    }

    /// <summary>Payroll summary list for a previously generated period.</summary>
    /// <remarks>Returns an empty array if no payroll has been generated for the exact period, or if the period had no activity.</remarks>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<PayrollSummaryDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<PayrollSummaryDto>>> GetSummary(
        [FromQuery, Required] DateOnly startDate,
        [FromQuery, Required] DateOnly endDate,
        CancellationToken ct)
        => Ok(await payroll.GetSummaryAsync(startDate, endDate, ct));

    /// <summary>Detailed payroll breakdown for one instructor in a period.</summary>
    /// <remarks>Includes classes taught, commission records, refund adjustments, bonus records, payout adjustments and the full audit ledger.</remarks>
    [HttpGet("{instructorId:guid}")]
    [ProducesResponseType<InstructorPayrollDetailDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<InstructorPayrollDetailDto>> GetInstructorDetail(
        Guid instructorId,
        [FromQuery, Required] DateOnly startDate,
        [FromQuery, Required] DateOnly endDate,
        CancellationToken ct)
        => Ok(await payroll.GetInstructorDetailAsync(instructorId, startDate, endDate, ct));

    /// <summary>List all generated payroll runs (newest first). Convenience endpoint for the dashboard.</summary>
    [HttpGet("runs")]
    [ProducesResponseType<IReadOnlyList<PayrollRunDto>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PayrollRunDto>>> ListRuns(CancellationToken ct)
        => Ok(await payroll.ListRunsAsync(ct));
}
