namespace ArchLucid.Decisioning.Advisory.Workflow;

/// <summary>
///     Allowed values for <see cref="RecommendationActionRequest.Action" /> when posting to the advisory recommendations
///     action endpoint.
/// </summary>
public static class RecommendationActionType
{
    public const string Accept = "Accept";
    public const string Reject = "Reject";
    public const string Defer = "Defer";
    public const string MarkImplemented = "MarkImplemented";
}
