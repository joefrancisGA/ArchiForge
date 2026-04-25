using ArchLucid.Decisioning.Models;

using Cm = ArchLucid.Contracts.Manifest;

namespace ArchLucid.Decisioning.Interfaces;

/// <summary>
///     Maps an Authority pipeline <see cref="Models.GoldenManifest" /> to the operator / API
///     <see cref="Cm.GoldenManifest" /> shape (Coordinator contract).
/// </summary>
public interface IAuthorityCommitProjectionBuilder
{
    /// <summary>
    ///     Build the coordinator-shaped manifest. <paramref name="input" />.
    ///     <see cref="AuthorityCommitProjectionInput.SystemName" />
    ///     is supplied by the application layer (usually from <c>ArchitectureRequest</c> / run header), not read here,
    ///     to keep <c>ArchLucid.Decisioning</c> free of persistence references.
    /// </summary>
    Task<Cm.GoldenManifest> BuildAsync(
        GoldenManifest source,
        AuthorityCommitProjectionInput input,
        CancellationToken cancellationToken = default);
}
