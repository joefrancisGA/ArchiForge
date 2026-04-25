namespace ArchLucid.Decisioning.Alerts.Delivery;

/// <summary>
///     Orders severities for subscription filtering: <see cref="AlertRoutingSubscription.MinimumSeverity" /> vs
///     <see cref="AlertRecord.Severity" />.
/// </summary>
/// <remarks>
///     Uses ranks from <see cref="AlertSeverity" /> constants. Unknown strings map to rank <c>0</c> (below Info).
/// </remarks>
public static class AlertSeverityComparer
{
    /// <summary>Returns numeric rank for comparison; higher means more severe.</summary>
    /// <param name="severity">Typically <see cref="AlertSeverity" /> value or DB string.</param>
    public static int ToRank(string severity)
    {
        return severity switch
        {
            AlertSeverity.Info => 1,
            AlertSeverity.Warning => 2,
            AlertSeverity.High => 3,
            AlertSeverity.Critical => 4,
            _ => 0
        };
    }

    /// <summary>
    ///     <c>true</c> when <paramref name="actualSeverity" /> is at least as severe as
    ///     <paramref name="minimumSeverity" />.
    /// </summary>
    public static bool MeetsMinimum(string actualSeverity, string minimumSeverity)
    {
        return ToRank(actualSeverity) >= ToRank(minimumSeverity);
    }
}
