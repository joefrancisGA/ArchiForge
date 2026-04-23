namespace ArchLucid.Core.Configuration;

/// <summary>ADR 0030 PR A2 — global rollback switch between coordinator and authority run-commit implementations.</summary>
public sealed class LegacyRunCommitPathOptions
{
    /// <summary>Binds <c>Coordinator</c> section in configuration.</summary>
    public const string SectionPath = "Coordinator";

    /// <summary>
    /// When <see langword="true"/>, <see cref="ArchLucid.Application.Runs.Orchestration.ArchitectureRunCommitOrchestrator"/>
    /// handles commits (legacy JSON table). When <see langword="false"/>, authority path is used.
    /// </summary>
    public bool LegacyRunCommitPath { get; set; }
}
