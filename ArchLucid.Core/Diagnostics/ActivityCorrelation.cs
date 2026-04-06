using System.Diagnostics;

namespace ArchiForge.Core.Diagnostics;

/// <summary>
/// Helpers for propagating a logical correlation id across <see cref="Activity"/> hierarchies (OTel / diagnostics).
/// </summary>
/// <remarks>
/// HTTP requests set <see cref="LogicalCorrelationIdTag"/> in <c>CorrelationIdMiddleware</c>. Child activities started
/// during the request may become <see cref="Activity.Current"/>; audit enrichment walks the parent chain so
/// <see cref="ArchiForge.Core.Audit.AuditEvent.CorrelationId"/> still matches the client <c>X-Correlation-ID</c> when unset.
/// </remarks>
public static class ActivityCorrelation
{
    /// <summary>Aligned with common tracing semantics and <c>CorrelationIdMiddleware</c>.</summary>
    public const string LogicalCorrelationIdTag = "correlation.id";

    /// <summary>Returns the first non-empty <paramref name="tagName"/> value walking from <paramref name="start"/> to parents.</summary>
    public static string? FindTagValueInChain(Activity? start, string tagName)
    {
        if (string.IsNullOrWhiteSpace(tagName))
        
            throw new ArgumentException("Tag name is required.", nameof(tagName));
        

        for (Activity? activity = start; activity is not null; activity = activity.Parent)
        
            if (activity.GetTagItem(tagName) is string value && !string.IsNullOrWhiteSpace(value))
            
                return value;
            
        

        return null;
    }
}
