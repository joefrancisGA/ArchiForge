using System.Text.Json;
using ArchLucid.AgentRuntime;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Findings;

using ArchLucid.Application.QuickScan;

namespace ArchLucid.AgentRuntime.QuickScan;

/// <inheritdoc cref="IQuickScanService" />
public sealed class QuickScanService(IAgentCompletionClient completionClient) : IQuickScanService
{
    private const string SystemPrompt = """
        You are a lightweight architecture scanner. Analyze the provided file contents and return a JSON object with:
        - "summary": A high-level string summary of the architecture.
        - "findings": An array of objects with "category", "message", and "severity" (Info, Low, Medium, High, Critical).
        Do not include any markdown formatting, only return raw JSON.
        """;

    public async Task<QuickScanResult> ScanAsync(IReadOnlyDictionary<string, string> files, CancellationToken cancellationToken = default)
    {
        if (files is null) throw new ArgumentNullException(nameof(files));
        if (completionClient is null) throw new ArgumentNullException(nameof(completionClient));

        string userPrompt = JsonSerializer.Serialize(files);
        string jsonResponse = await completionClient.CompleteJsonAsync(SystemPrompt, userPrompt, cancellationToken);

        if (string.IsNullOrWhiteSpace(jsonResponse))
            return new QuickScanResult { Summary = "No response from LLM." };

        try
        {
            using JsonDocument doc = JsonDocument.Parse(jsonResponse);
            JsonElement root = doc.RootElement;

            string summary = root.TryGetProperty("summary", out JsonElement summaryElement)
                ? summaryElement.GetString() ?? string.Empty
                : string.Empty;

            List<ArchitectureFinding> findings = [];
            if (root.TryGetProperty("findings", out JsonElement findingsElement) && findingsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement findingElement in findingsElement.EnumerateArray())
                {
                    string category = findingElement.TryGetProperty("category", out JsonElement c) ? c.GetString() ?? "General" : "General";
                    string message = findingElement.TryGetProperty("message", out JsonElement m) ? m.GetString() ?? string.Empty : string.Empty;
                    string severityStr = findingElement.TryGetProperty("severity", out JsonElement s) ? s.GetString() ?? "Info" : "Info";

                    if (!Enum.TryParse(severityStr, true, out FindingSeverity severity))
                        severity = FindingSeverity.Info;

                    findings.Add(new ArchitectureFinding
                    {
                        Category = category,
                        Message = message,
                        Severity = severity,
                        FindingId = Guid.NewGuid().ToString("N"),
                        SourceAgent = AgentType.Topology // Using Topology as a generic source for quick scan
                    });
                }
            }

            return new QuickScanResult
            {
                Summary = summary,
                Findings = findings
            };
        }
        catch (JsonException)
        {
            return new QuickScanResult { Summary = "Failed to parse LLM response as JSON." };
        }
    }
}
