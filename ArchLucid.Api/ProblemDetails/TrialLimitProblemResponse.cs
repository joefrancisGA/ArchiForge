using System.Text.Json;

using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Core.Tenancy;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.ProblemDetails;

/// <summary>Builds RFC 9457 trial-limit responses and optional durable audit rows.</summary>
internal static class TrialLimitProblemResponse
{
    internal static async Task WriteResponseAsync(HttpContext httpContext, TrialLimitExceededException ex)
    {
        ObjectResult body = CreateResult(ex, httpContext.Request.Path.Value, httpContext);
        httpContext.Response.StatusCode = body.StatusCode ?? StatusCodes.Status402PaymentRequired;
        httpContext.Response.ContentType = ApplicationProblemMapper.ProblemJsonMediaType;

        if (body.Value is Microsoft.AspNetCore.Mvc.ProblemDetails p)
        {
            await httpContext.Response.WriteAsJsonAsync(p, cancellationToken: httpContext.RequestAborted);
        }

        await TryLogAuditAsync(httpContext, ex, httpContext.RequestAborted);
    }

    internal static ObjectResult CreateResult(TrialLimitExceededException ex, string? instance, HttpContext? httpContext)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.TrialExpired,
            Title = "Trial limit reached",
            Status = StatusCodes.Status402PaymentRequired,
            Detail = ex.Message,
            Instance = string.IsNullOrWhiteSpace(instance) ? null : instance,
        };

        ProblemErrorCodes.AttachErrorCode(problem, ProblemTypes.TrialExpired);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, httpContext);

        problem.Extensions["traceCompleteness"] = new
        {
            totalFindings = 0,
            overallCompletenessRatio = 0.0,
            byEngine = new Dictionary<string, object>(),
        };

        problem.Extensions["trialReason"] = ex.Reason.ToString();

        problem.Extensions["daysRemaining"] = ex.DaysRemaining;

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType },
        };
    }

    internal static async Task TryLogAuditAsync(HttpContext httpContext, TrialLimitExceededException ex, CancellationToken ct)
    {
        try
        {
            IAuditService audit = httpContext.RequestServices.GetRequiredService<IAuditService>();
            IScopeContextProvider scopeProvider = httpContext.RequestServices.GetRequiredService<IScopeContextProvider>();
            ScopeContext scope = scopeProvider.GetCurrentScope();

            string actor =
                httpContext.User?.FindFirst("sub")?.Value
                ?? httpContext.User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
                ?? httpContext.User?.Identity?.Name
                ?? "unknown";

            await audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialLimitExceeded,
                    ActorUserId = actor,
                    ActorUserName = actor,
                    TenantId = scope.TenantId,
                    WorkspaceId = scope.WorkspaceId,
                    ProjectId = scope.ProjectId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            trialReason = ex.Reason.ToString(),
                            daysRemaining = ex.DaysRemaining,
                            detail = ex.Message,
                        }),
                },
                ct);
        }
        catch
        {
            // Audit must never mask the primary 402 response.
        }
    }
}
