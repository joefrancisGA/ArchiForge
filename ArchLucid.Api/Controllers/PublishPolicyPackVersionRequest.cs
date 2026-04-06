namespace ArchiForge.Api.Controllers;

/// <summary>Body for <c>POST …/policy-packs/{id}/publish</c>.</summary>
/// <remarks><see cref="Version"/> must satisfy SemVer rules enforced by validator.</remarks>
public sealed class PublishPolicyPackVersionRequest
{
    /// <summary>Version label to publish or update in place.</summary>
    public string Version { get; set; } = null!;

    /// <summary>Full pack content JSON.</summary>
    public string ContentJson { get; set; } = "{}";
}
