using System.Text;

using ArchLucid.Core.Audit;

namespace ArchLucid.Cli.Commands;

/// <summary>Operator hints when <c>archlucid second-run</c> fails mid-flight (grep logs by correlation id + audit type).</summary>
internal static class SecondRunDiagnostics
{
    /// <summary>Writes correlation id (when known) and canonical audit event names to search in host logs.</summary>
    internal static async Task WriteAsync(TextWriter writer, string step, int? httpStatus, string? correlationId, string? apiDetail)
    {
        ArgumentNullException.ThrowIfNull(writer);

        await writer.WriteLineAsync();
        await writer.WriteLineAsync($"Second-run failed at step: {step}");

        if (httpStatus is { } code)
            await writer.WriteLineAsync($"HTTP status: {code}");

        if (!string.IsNullOrWhiteSpace(apiDetail))
            await writer.WriteLineAsync($"API detail: {apiDetail}");

        if (!string.IsNullOrWhiteSpace(correlationId))
        {
            await writer.WriteLineAsync();
            await writer.WriteLineAsync($"Correlation id (grep Application Insights / host logs): {correlationId}");
        }

        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Audit event names to grep (architecture run lifecycle):");
        await writer.WriteLineAsync(BuildAuditLine());
        await writer.WriteLineAsync();
        await writer.WriteLineAsync("Canonical authority twins (dual-write era):");
        await writer.WriteLineAsync(
            $"  {AuditEventTypes.Run.Created}, {AuditEventTypes.Run.ExecuteStarted}, {AuditEventTypes.Run.ExecuteSucceeded}, {AuditEventTypes.Run.CommitCompleted}, {AuditEventTypes.Run.Failed}");
    }

    private static string BuildAuditLine()
    {
        StringBuilder sb = new();

        sb.Append("  ")
            .Append(AuditEventTypes.Baseline.Architecture.RunCreated)
            .Append(", ")
            .Append(AuditEventTypes.Baseline.Architecture.RunStarted)
            .Append(", ")
            .Append(AuditEventTypes.Baseline.Architecture.RunExecuteSucceeded)
            .Append(", ")
            .Append(AuditEventTypes.Baseline.Architecture.RunCompleted)
            .Append(", ")
            .Append(AuditEventTypes.Baseline.Architecture.RunFailed);

        return sb.ToString();
    }
}
