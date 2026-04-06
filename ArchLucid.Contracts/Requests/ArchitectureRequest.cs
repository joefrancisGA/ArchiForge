using System.ComponentModel.DataAnnotations;

using ArchiForge.Contracts.Common;

namespace ArchiForge.Contracts.Requests;

/// <summary>
/// Describes a request for an architecture analysis run. Contains all input signals
/// an AI agent needs to propose and evaluate a target architecture, including the system
/// description, constraints, evidence documents, policy references, and topology hints.
/// </summary>
public sealed class ArchitectureRequest
{
    /// <summary>Stable client-supplied identifier for this request (max 64 characters).</summary>
    [Required]
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// Natural-language description of the system and what the architecture must achieve.
    /// Minimum 10 characters; maximum 4 000 characters.
    /// </summary>
    [Required]
    [MinLength(10)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Short name for the system being designed (e.g. <c>OrderService</c>).</summary>
    [Required]
    public string SystemName { get; set; } = string.Empty;

    /// <summary>Target deployment environment (e.g. <c>prod</c>, <c>staging</c>).</summary>
    [Required]
    public string Environment { get; set; } = "prod";

    /// <summary>Primary cloud provider for the target architecture.</summary>
    [Required]
    public CloudProvider CloudProvider { get; set; } = CloudProvider.Azure;

    /// <summary>Hard constraints the architecture must satisfy (max 50 items).</summary>
    public List<string> Constraints { get; set; } = [];

    /// <summary>Capabilities the architecture is required to support (max 50 items).</summary>
    public List<string> RequiredCapabilities { get; set; } = [];

    /// <summary>Assumptions that agents may rely on when proposing the architecture (max 50 items).</summary>
    public List<string> Assumptions { get; set; } = [];

    /// <summary>
    /// Version string of an existing manifest that agents should use as a baseline when
    /// proposing incremental changes. <see langword="null"/> for greenfield runs.
    /// </summary>
    public string? PriorManifestVersion { get; set; }

    /// <summary>Free-text requirements supplementing the description (max 100 items, each max 4 000 characters).</summary>
    public List<string> InlineRequirements { get; set; } = [];

    /// <summary>
    /// Attached reference documents (e.g. ADRs, RFCs, runbooks) provided as context for agents.
    /// Max 50 documents.
    /// </summary>
    public List<ContextDocumentRequest> Documents { get; set; } = [];

    /// <summary>References to policy packs that must be evaluated against the proposed architecture (max 100 items).</summary>
    public List<string> PolicyReferences { get; set; } = [];

    /// <summary>Hints about which topology patterns to prefer or avoid (max 100 items).</summary>
    public List<string> TopologyHints { get; set; } = [];

    /// <summary>Hints about security baseline requirements for the architecture (max 100 items).</summary>
    public List<string> SecurityBaselineHints { get; set; } = [];

    /// <summary>
    /// Existing infrastructure declarations (IaC, service mesh config, etc.) that agents
    /// should incorporate or reason about (max 50 items).
    /// </summary>
    public List<InfrastructureDeclarationRequest> InfrastructureDeclarations { get; set; } = [];
}
