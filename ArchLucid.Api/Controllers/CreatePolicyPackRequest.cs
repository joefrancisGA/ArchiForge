namespace ArchiForge.Api.Controllers;

/// <summary>Body for <c>POST …/policy-packs</c> (create pack + initial draft version).</summary>
/// <remarks>Validated by FluentValidation; <see cref="PackType"/> must be a known <c>PolicyPackType</c> string.</remarks>
public sealed class CreatePolicyPackRequest
{
    /// <summary>Display name (required).</summary>
    public string Name { get; set; } = null!;

    /// <summary>Optional description.</summary>
    public string Description { get; set; } = "";

    /// <summary>E.g. <c>ProjectCustom</c>, <c>TenantCustom</c>.</summary>
    public string PackType { get; set; } = null!;

    /// <summary>JSON object matching <c>PolicyPackContentDocument</c> shape for version <c>1.0.0</c> draft.</summary>
    public string InitialContentJson { get; set; } = "{}";
}
