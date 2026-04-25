using ArchLucid.Contracts.Agents;
using ArchLucid.Decisioning.Validation;

namespace ArchLucid.Decisioning.Merge;

/// <summary>
///     Validates merge inputs, filters invalid <see cref="AgentResult" /> rows, and runs per-result JSON schema checks.
/// </summary>
public sealed class DecisionMergeInputGate(ISchemaValidationService schemaValidationService)
{
    private readonly ISchemaValidationService _schemaValidationService =
        schemaValidationService ?? throw new ArgumentNullException(nameof(schemaValidationService));

    public bool TryValidateMergeInputs(
        string runId,
        string manifestVersion,
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        if (string.IsNullOrWhiteSpace(runId))
        {
            output.Errors.Add("RunId is required.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(manifestVersion))
        {
            output.Errors.Add("Manifest version is required.");
            return false;
        }

        if (results.Count != 0)
            return true;

        output.Errors.Add("At least one agent result is required.");
        return false;
    }

    public List<AgentResult> ValidateAndFilterResults(
        string runId,
        IReadOnlyCollection<AgentResult> results,
        DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(results);
        ArgumentNullException.ThrowIfNull(output);

        List<AgentResult> valid = [];

        foreach (AgentResult result in results)
        {
            List<string> errors = ValidateResult(result, runId);

            if (errors.Count > 0)
            {
                output.Errors.AddRange(errors.Select(e => $"AgentResult {result.ResultId}: {e}"));
                continue;
            }

            valid.Add(result);
        }

        return valid;
    }

    public bool ValidateAgentResultsAgainstSchema(List<AgentResult> validResults, DecisionMergeResult output)
    {
        ArgumentNullException.ThrowIfNull(validResults);
        ArgumentNullException.ThrowIfNull(output);

        foreach (AgentResult result in validResults)
        {
            string resultJson = SchemaValidationSerializer.Serialize(result);
            SchemaValidationResult schemaValidation = _schemaValidationService.ValidateAgentResultJson(resultJson);

            if (schemaValidation.IsValid)
                continue;

            foreach (string error in schemaValidation.Errors)
                output.Errors.Add($"AgentResult {result.ResultId}: {error}");
        }

        return output.Errors.Count == 0;
    }

    private static List<string> ValidateResult(AgentResult result, string runId)
    {
        List<string> errors = [];

        if (string.IsNullOrWhiteSpace(result.ResultId))
            errors.Add("ResultId is required.");

        if (string.IsNullOrWhiteSpace(result.TaskId))
            errors.Add("TaskId is required.");

        if (string.IsNullOrWhiteSpace(result.RunId))
            errors.Add("RunId is required.");
        else if (!string.Equals(result.RunId, runId, StringComparison.Ordinal))
            errors.Add(
                $"RunId '{result.RunId}' does not match the merge run '{runId}'; cross-run results must not be merged.");

        if (result.Confidence < 0.0 || result.Confidence > 1.0)
            errors.Add("Confidence must be between 0 and 1.");

        return errors;
    }
}
