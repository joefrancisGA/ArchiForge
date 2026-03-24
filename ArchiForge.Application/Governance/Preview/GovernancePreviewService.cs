using ArchiForge.Contracts.Governance;
using ArchiForge.Contracts.Governance.Preview;
using ArchiForge.Contracts.Manifest;
using ArchiForge.Data.Repositories;

namespace ArchiForge.Application.Governance.Preview;

/// <summary>
/// Read-only governance preview: compares manifest governance without persisting activations or workflow state.
/// Run and manifest access is routed through <see cref="IRunDetailQueryService"/> so governance preview
/// shares the same canonical run detail path as export and compare features.
/// </summary>
public sealed class GovernancePreviewService(
    IGovernanceEnvironmentActivationRepository activationRepository,
    IRunDetailQueryService runDetailQueryService,
    IGoldenManifestRepository manifestRepository)
    : IGovernancePreviewService
{
    public async Task<GovernancePreviewResult> PreviewActivationAsync(
        GovernancePreviewRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.RunId))
            throw new ArgumentException("RunId is required.", nameof(request));
        if (string.IsNullOrWhiteSpace(request.ManifestVersion))
            throw new ArgumentException("ManifestVersion is required.", nameof(request));

        var environment = NormalizeAndValidateEnvironment(request.Environment, nameof(request.Environment));

        // Use the canonical run detail path to validate run existence and load its manifest.
        var runDetail = await runDetailQueryService.GetRunDetailAsync(request.RunId, cancellationToken)
            ?? throw new RunNotFoundException(request.RunId);

        // The candidate manifest is the specific version being previewed — it may differ from
        // the run's current committed manifest (e.g. an older committed version is being evaluated).
        var candidateManifest = runDetail.Manifest is not null
            && string.Equals(runDetail.Run.CurrentManifestVersion, request.ManifestVersion, StringComparison.Ordinal)
                ? runDetail.Manifest
                : await manifestRepository.GetByVersionAsync(request.ManifestVersion, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"Golden manifest version '{request.ManifestVersion}' was not found.");

        if (!string.Equals(candidateManifest.RunId, request.RunId, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                $"Manifest version '{request.ManifestVersion}' belongs to run '{candidateManifest.RunId}', not '{request.RunId}'.");
        }

        var activationRows = await activationRepository.GetByEnvironmentAsync(environment, cancellationToken);
        var active = activationRows.FirstOrDefault(a => a.IsActive);

        GoldenManifest? currentManifest = null;
        if (active is not null)
            currentManifest = await manifestRepository.GetByVersionAsync(active.ManifestVersion, cancellationToken);

        var notes = new List<string>
        {
            "Only governance keys that differ are listed; unchanged keys are omitted."
        };

        if (active is null)
        {
            notes.Add($"No current active governance activation exists for environment '{environment}'.");
            notes.Add("Preview compares candidate governance against empty current state.");
        }
        else
        {
            notes.Add(
                $"Compared current run '{active.RunId}' (manifest '{active.ManifestVersion}') to preview run '{request.RunId}' (manifest '{request.ManifestVersion}').");
            if (currentManifest is null)
            {
                notes.Add(
                    $"Could not load GoldenManifest for current activation manifest version '{active.ManifestVersion}'.");
            }
        }

        var differences = GovernanceManifestComparer.Compare(
            currentManifest?.Governance,
            candidateManifest.Governance);

        return new GovernancePreviewResult
        {
            Environment = environment,
            CurrentRunId = active?.RunId,
            CurrentManifestVersion = active?.ManifestVersion,
            PreviewRunId = request.RunId,
            PreviewManifestVersion = request.ManifestVersion,
            Differences = differences,
            Notes = notes
        };
    }

    public async Task<GovernanceEnvironmentComparisonResult> CompareEnvironmentsAsync(
        GovernanceEnvironmentComparisonRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var source = NormalizeAndValidateEnvironment(request.SourceEnvironment, nameof(request.SourceEnvironment));
        var target = NormalizeAndValidateEnvironment(request.TargetEnvironment, nameof(request.TargetEnvironment));

        if (string.Equals(source, target, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "SourceEnvironment and TargetEnvironment must be different.",
                nameof(request));
        }

        var notes = new List<string>
        {
            "Only governance keys that differ are listed; unchanged keys are omitted."
        };

        var sourceRows = await activationRepository.GetByEnvironmentAsync(source, cancellationToken);
        var targetRows = await activationRepository.GetByEnvironmentAsync(target, cancellationToken);
        var sourceActive = sourceRows.FirstOrDefault(a => a.IsActive);
        var targetActive = targetRows.FirstOrDefault(a => a.IsActive);

        if (sourceActive is null)
            notes.Add($"No active governance activation exists for source environment '{source}'.");
        if (targetActive is null)
            notes.Add($"No active governance activation exists for target environment '{target}'.");

        GoldenManifest? sourceManifest = sourceActive is not null
            ? await manifestRepository.GetByVersionAsync(sourceActive.ManifestVersion, cancellationToken)
            : null;
        GoldenManifest? targetManifest = targetActive is not null
            ? await manifestRepository.GetByVersionAsync(targetActive.ManifestVersion, cancellationToken)
            : null;

        if (sourceActive is not null && sourceManifest is null)
        {
            notes.Add(
                $"Could not load GoldenManifest for source manifest version '{sourceActive.ManifestVersion}'.");
        }

        if (targetActive is not null && targetManifest is null)
        {
            notes.Add(
                $"Could not load GoldenManifest for target manifest version '{targetActive.ManifestVersion}'.");
        }

        if (sourceActive is not null && targetActive is not null && sourceManifest is not null && targetManifest is not null)
            notes.Add($"Compared active governance states for environments '{source}' and '{target}'.");

        var differences = GovernanceManifestComparer.Compare(
            sourceManifest?.Governance,
            targetManifest?.Governance);

        return new GovernanceEnvironmentComparisonResult
        {
            SourceEnvironment = source,
            TargetEnvironment = target,
            Differences = differences,
            Notes = notes
        };
    }

    private static string NormalizeAndValidateEnvironment(string environment, string paramName)
    {
        if (string.IsNullOrWhiteSpace(environment))
            throw new ArgumentException("Environment is required.", paramName);

        if (!IsKnownEnvironment(environment))
        {
            throw new ArgumentException(
                "Environment must be one of: dev, test, prod.",
                paramName);
        }

        return environment.Trim().ToLowerInvariant();
    }

    private static bool IsKnownEnvironment(string value) =>
        string.Equals(value, GovernanceEnvironment.Dev, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, GovernanceEnvironment.Test, StringComparison.OrdinalIgnoreCase)
        || string.Equals(value, GovernanceEnvironment.Prod, StringComparison.OrdinalIgnoreCase);
}
