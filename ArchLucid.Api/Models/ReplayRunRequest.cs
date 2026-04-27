using System.Diagnostics.CodeAnalysis;

using ArchLucid.Application;

namespace ArchLucid.Api.Models;

[ExcludeFromCodeCoverage(Justification = "API request/response DTO; no business logic.")]
public sealed class ReplayRunRequest
{
    public bool CommitReplay
    {
        get;
        set;
    } = false;

    public string ExecutionMode
    {
        get;
        set;
    } = ExecutionModes.Current;

    public string? ManifestVersionOverride
    {
        get;
        set;
    }
}
