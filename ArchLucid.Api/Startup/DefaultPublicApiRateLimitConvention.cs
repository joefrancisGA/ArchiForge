using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Startup;

/// <summary>
///     Ensures every controller action participates in fixed-window API throttling unless rate limiting metadata is
///     already declared on the selector, controller, or action.
/// </summary>
internal sealed class DefaultPublicApiRateLimitConvention : IApplicationModelConvention
{
    /// <inheritdoc />
    public void Apply(ApplicationModel application)
    {
        foreach (ControllerModel controller in application.Controllers)
        {
            foreach (ActionModel action in controller.Actions)
            {
                if (HasExistingRateLimitMetadata(controller, action))
                    continue;


                foreach (SelectorModel selector in action.Selectors)
                    selector.EndpointMetadata.Add(new EnableRateLimitingAttribute("fixed"));
            }
        }
    }

    private static bool HasExistingRateLimitMetadata(ControllerModel controller, ActionModel action)
    {
        if (MetadataFromAttributes(controller.Attributes))
            return true;

        if (MetadataFromAttributes(action.Attributes))
            return true;

        if (SelectorsHaveRateLimiting(action.Selectors))
            return true;

        return false;
    }

    private static bool MetadataFromAttributes(IReadOnlyList<object> attributes)
    {
        foreach (object attr in attributes)
        {
            if (attr is EnableRateLimitingAttribute)
                return true;

            if (attr is DisableRateLimitingAttribute)
                return true;
        }

        return false;
    }

    private static bool SelectorsHaveRateLimiting(IList<SelectorModel> selectors)
    {
        foreach (SelectorModel selector in selectors)
        {
            foreach (object md in selector.EndpointMetadata)
            {
                if (md is EnableRateLimitingAttribute)
                    return true;

                if (md is DisableRateLimitingAttribute)
                    return true;
            }
        }

        return false;
    }
}
