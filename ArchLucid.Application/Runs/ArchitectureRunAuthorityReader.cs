using ArchLucid.Application.Runs.Mapping;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Metadata;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Data.Repositories;
using ArchLucid.Persistence.Interfaces;
using ArchLucid.Persistence.Models;

namespace ArchLucid.Application.Runs;

/// <summary>
/// Loads <see cref="ArchitectureRun"/> from <see cref="IRunRepository"/> within the current <see cref="ScopeContext"/>.
/// </summary>
public static class ArchitectureRunAuthorityReader
{
    /// <summary>
    /// Returns <see langword="null"/> when <paramref name="runId"/> is not a GUID, the run is outside scope, or it is archived.
    /// </summary>
    public static async Task<ArchitectureRun?> TryGetArchitectureRunAsync(
        IRunRepository runRepository,
        IScopeContextProvider scopeContextProvider,
        IAgentTaskRepository taskRepository,
        string runId,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runRepository);
        ArgumentNullException.ThrowIfNull(scopeContextProvider);
        ArgumentNullException.ThrowIfNull(taskRepository);
        ArgumentException.ThrowIfNullOrWhiteSpace(runId);

        if (!TryParseRunGuid(runId, out Guid runGuid))
        {
            return null;
        }

        ScopeContext scope = scopeContextProvider.GetCurrentScope();

        RunRecord? record = await runRepository.GetByIdAsync(scope, runGuid, cancellationToken);

        if (record is null)
        {
            return null;
        }

        IReadOnlyList<AgentTask>? tasks = await taskRepository.GetByRunIdAsync(runId, cancellationToken);

        IReadOnlyList<string> taskIds = tasks is null
            ? []
            : tasks.Select(t => t.TaskId).ToList();

        return RunRecordToArchitectureRunMapper.ToArchitectureRun(record, taskIds);
    }

    private static bool TryParseRunGuid(string runId, out Guid runGuid)
    {
        if (Guid.TryParseExact(runId, "N", out runGuid))
        {
            return true;
        }

        return Guid.TryParse(runId, out runGuid);
    }
}
