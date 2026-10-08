using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Rezerv.Payroll.Application.Exceptions;

namespace Rezerv.Payroll.Api.Infrastructure;

/// <summary>
/// Maps application exceptions to RFC 7807 ProblemDetails so the frontend gets a consistent,
/// machine-readable error shape with a human-readable <c>detail</c>.
/// </summary>
public sealed class PayrollExceptionHandler(IProblemDetailsService problemDetails, ILogger<PayrollExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title, extensions) = exception switch
        {
            PayrollValidationException => (StatusCodes.Status400BadRequest, "Invalid payroll request", null),
            PayrollNotFoundException => (StatusCodes.Status404NotFound, "Payroll not found", null),
            PayrollPeriodOverlapException ex => (StatusCodes.Status409Conflict, "Payroll period overlaps an existing run",
                new Dictionary<string, object?>
                {
                    ["conflictingPeriods"] = ex.Conflicts.Select(c => new { startDate = c.StartDate, endDate = c.EndDate }).ToList(),
                }),
            DuplicatePayrollRunException => (StatusCodes.Status409Conflict, "Payroll already generated concurrently", null),
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "Malformed request", null),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected error", null),
        };

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        var details = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = status == StatusCodes.Status500InternalServerError ? "An unexpected error occurred." : exception.Message,
            Instance = httpContext.Request.Path,
        };
        if (extensions is not null)
        {
            foreach (var (k, v) in extensions)
            {
                details.Extensions[k] = v;
            }
        }

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = details,
            Exception = exception,
        });
    }
}
