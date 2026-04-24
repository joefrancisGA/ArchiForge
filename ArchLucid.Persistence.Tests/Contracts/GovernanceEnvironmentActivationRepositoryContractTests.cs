using ArchLucid.Contracts.Governance;
using ArchLucid.Persistence.Data.Repositories;

using FluentAssertions;

namespace ArchLucid.Persistence.Tests.Contracts;

/// <summary>
///     Shared contract assertions for <see cref="IGovernanceEnvironmentActivationRepository" />.
/// </summary>
public abstract class GovernanceEnvironmentActivationRepositoryContractTests
{
    protected virtual void SkipIfSqlServerUnavailable()
    {
    }

    protected abstract IGovernanceEnvironmentActivationRepository CreateRepository();

    [SkippableFact]
    public async Task Create_then_GetByEnvironment_returns_row_newest_first()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceEnvironmentActivationRepository repo = CreateRepository();
        string env = "dev-" + Guid.NewGuid().ToString("N")[..8];
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        await repo.CreateAsync(NewActivation("act-old", "run-a", env, older, true), CancellationToken.None);
        await repo.CreateAsync(NewActivation("act-new", "run-b", env, newer, true), CancellationToken.None);

        IReadOnlyList<GovernanceEnvironmentActivation> list =
            await repo.GetByEnvironmentAsync(env, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].ActivationId.Should().Be("act-new");
        list[1].ActivationId.Should().Be("act-old");
    }

    [SkippableFact]
    public async Task Update_changes_only_IsActive_visible_in_GetByRunId()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceEnvironmentActivationRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string activationId = "act-upd-" + Guid.NewGuid().ToString("N");

        GovernanceEnvironmentActivation created =
            NewActivation(activationId, runId, GovernanceEnvironment.Dev, DateTime.UtcNow, true);
        created.ManifestVersion = "v-before";

        await repo.CreateAsync(created, CancellationToken.None);

        GovernanceEnvironmentActivation patch = new()
        {
            ActivationId = activationId,
            RunId = runId,
            ManifestVersion = "v-after",
            Environment = GovernanceEnvironment.Test,
            IsActive = false,
            ActivatedUtc = new DateTime(2099, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };

        await repo.UpdateAsync(patch, CancellationToken.None);

        IReadOnlyList<GovernanceEnvironmentActivation> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        GovernanceEnvironmentActivation? row = list.SingleOrDefault(x => x.ActivationId == activationId);
        row.Should().NotBeNull();
        row.IsActive.Should().BeFalse();
        row.ManifestVersion.Should().Be("v-before");
        row.Environment.Should().Be(GovernanceEnvironment.Dev);
    }

    [SkippableFact]
    public async Task GetByRunId_orders_descending_by_ActivatedUtc()
    {
        SkipIfSqlServerUnavailable();
        IGovernanceEnvironmentActivationRepository repo = CreateRepository();
        string runId = Guid.NewGuid().ToString("N");
        string env = "test";
        DateTime older = new(2026, 4, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newer = new(2026, 4, 1, 11, 0, 0, DateTimeKind.Utc);

        await repo.CreateAsync(NewActivation("a-old", runId, env, older, false), CancellationToken.None);
        await repo.CreateAsync(NewActivation("a-new", runId, env, newer, true), CancellationToken.None);

        IReadOnlyList<GovernanceEnvironmentActivation> list = await repo.GetByRunIdAsync(runId, CancellationToken.None);

        list.Should().HaveCount(2);
        list[0].ActivationId.Should().Be("a-new");
        list[1].ActivationId.Should().Be("a-old");
    }

    private static GovernanceEnvironmentActivation NewActivation(
        string activationId,
        string runId,
        string environment,
        DateTime activatedUtc,
        bool isActive)
    {
        return new GovernanceEnvironmentActivation
        {
            ActivationId = activationId,
            RunId = runId,
            ManifestVersion = "v1",
            Environment = environment,
            IsActive = isActive,
            ActivatedUtc = activatedUtc
        };
    }
}
