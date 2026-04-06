using ArchiForge.Contracts.Decisions;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Contracts.Metadata;

namespace ArchiForge.Decisioning.Merge;

/// <summary>
/// Result of a <see cref="IDecisionEngineService.MergeResults"/> call.
/// When <see cref="Success"/> is <see langword="true"/>, <see cref="Manifest"/> is fully populated
/// and ready to be persisted. When <see langword="false"/>, <see cref="Errors"/> contains at least
/// one entry and <see cref="Manifest"/> must not be used.
/// </summary>
public sealed class DecisionMergeResult
{
    /// <summary>
    /// The merged and schema-validated manifest. Only meaningful when <see cref="Success"/> is <see langword="true"/>.
    /// </summary>
    public GoldenManifest Manifest { get; set; } = new();

    /// <summary>
    /// Decision nodes resolved by the engine. Populated even when errors are present, up to the point of failure.
    /// </summary>
    public List<DecisionNode> Decisions { get; set; } = [];

    /// <summary>
    /// Non-fatal warnings raised during merge (e.g. duplicate topics, low confidence signals).
    /// A merge can succeed with warnings present.
    /// </summary>
    public List<string> Warnings { get; set; } = [];

    /// <summary>
    /// Fatal errors that prevented a successful merge. Non-empty implies <see cref="Success"/> is <see langword="false"/>.
    /// </summary>
    public List<string> Errors { get; set; } = [];

    /// <summary>
    /// Audit traces recording every decision step taken during the merge.
    /// Always populated regardless of success or failure.
    /// </summary>
    public List<RunEventTrace> DecisionTraces { get; set; } = [];

    /// <summary>
    /// <see langword="true"/> when <see cref="Errors"/> is empty and the merge completed successfully.
    /// </summary>
    public bool Success => Errors.Count == 0;
}
