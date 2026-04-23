using ArchLucid.Api.Attributes;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Core.Configuration;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Filters;

/// <summary>
///     Short-circuits an action with <c>404 Not Found</c> when the corresponding deployment-level feature
///     toggle is off. Mounted by <see cref="FeatureGateAttribute" />; the gate key is the first positional
///     argument and every other dependency is DI-resolved.
/// </summary>
/// <remarks>
///     404 (not 403) is intentional — production-like deployments must not even hint that demo surfaces exist.
/// </remarks>
public sealed class FeatureGateFilter(
    FeatureGateKey key,
    IOptions<DemoOptions> demoOptions) : IAsyncActionFilter
{
    private readonly IOptions<DemoOptions> _demoOptions =
        demoOptions ?? throw new ArgumentNullException(nameof(demoOptions));

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        if (context is null)
            throw new ArgumentNullException(nameof(context));
        if (next is null)
            throw new ArgumentNullException(nameof(next));

        if (IsGateOpen())
        {
            await next();

            return;
        }

        context.Result = BuildNotFoundResult(context);
    }

    /// <summary>
    ///     Resolves the feature-toggle backing the supplied gate key. Each new <see cref="FeatureGateKey" />
    ///     member must be added here — the discard arm closes by default so unmapped keys cannot accidentally
    ///     open a route.
    /// </summary>
    private bool IsGateOpen()
    {
        return key switch
        {
            FeatureGateKey.DemoEnabled => _demoOptions.Value?.Enabled is true,
            _ => false
        };
    }

    private static ObjectResult BuildNotFoundResult(ActionExecutingContext context)
    {
        Microsoft.AspNetCore.Mvc.ProblemDetails problem = new()
        {
            Type = ProblemTypes.ResourceNotFound,
            Title = "Not Found",
            Status = StatusCodes.Status404NotFound,
            Detail = "The requested route is not available on this deployment.",
            Instance = context.HttpContext.Request.Path.Value
        };

        ProblemErrorCodes.AttachErrorCode(problem, problem.Type);
        ProblemSupportHints.AttachForProblemType(problem);
        ProblemCorrelation.Attach(problem, context.HttpContext);

        return new ObjectResult(problem)
        {
            StatusCode = problem.Status,
            ContentTypes = { ApplicationProblemMapper.ProblemJsonMediaType }
        };
    }
}
