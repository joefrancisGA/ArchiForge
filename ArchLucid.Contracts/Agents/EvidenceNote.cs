namespace ArchiForge.Contracts.Agents;

/// <summary>
/// A typed annotation attached to an <see cref="AgentEvidencePackage"/> that communicates
/// pipeline-level signals to agents (e.g., execution mode, prior-manifest availability).
/// Well-known <see cref="NoteType"/> values are defined in
/// <c>ArchiForge.Application.Evidence.EvidenceNoteTypes</c>.
/// </summary>
public sealed class EvidenceNote
{
    /// <summary>
    /// Discriminator that identifies the kind of signal this note carries.
    /// Consumers should pattern-match on this value.
    /// </summary>
    public string NoteType { get; set; } = string.Empty;

    /// <summary>Human-readable message body for the note.</summary>
    public string Message { get; set; } = string.Empty;
}
