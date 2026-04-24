namespace ArchLucid.Application.Pilots;

/// <summary>Renders the execution provenance block for sponsor-facing pilot Markdown reports.</summary>
public interface IExecutionProvenanceFooterRenderer
{
    /// <summary>GitHub-style Markdown callout (yellow) when real execution was replaced by simulator output.</summary>
    string BuildYellowSimulatorSubstitutionCallout();

    /// <summary>Footer Markdown (heading + table) describing execution mode and observability proxies.</summary>
    string BuildFooterMarkdown(ExecutionProvenanceFooterInput input);
}
