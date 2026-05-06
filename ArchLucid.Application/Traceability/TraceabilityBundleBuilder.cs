using System.IO.Compression;
using System.Text.Json;
using ArchLucid.Contracts.Architecture;
using ArchLucid.Contracts.DecisionTraces;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Scoping;
using ArchLucid.Persistence.Audit;

namespace ArchLucid.Application.Traceability;
/// <inheritdoc/>
public sealed class TraceabilityBundleBuilder(IRunDetailQueryService runDetailQueryService, IAuditRepository auditRepository) : ITraceabilityBundleBuilder
{
    private readonly byte __primaryConstructorArgumentValidation = __ValidatePrimaryConstructorArguments(runDetailQueryService, auditRepository);
    private static byte __ValidatePrimaryConstructorArguments(ArchLucid.Application.IRunDetailQueryService runDetailQueryService, ArchLucid.Persistence.Audit.IAuditRepository auditRepository)
    {
        ArgumentNullException.ThrowIfNull(runDetailQueryService);
        ArgumentNullException.ThrowIfNull(auditRepository);
        return (byte)0;
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private readonly IAuditRepository _auditRepository = auditRepository ?? throw new ArgumentNullException(nameof(auditRepository));
    private readonly IRunDetailQueryService _runDetailQueryService = runDetailQueryService ?? throw new ArgumentNullException(nameof(runDetailQueryService));
    /// <inheritdoc/>
    public async System.Threading.Tasks.Task<System.Byte[]?> BuildAsync(string runId, ScopeContext scope, long maxZipBytes, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(runId);
        ArgumentNullException.ThrowIfNull(scope);
        if (string.IsNullOrWhiteSpace(runId))
            throw new ArgumentException("Run id is required.", nameof(runId));
        if (scope is null)
            throw new ArgumentNullException(nameof(scope));
        if (maxZipBytes <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxZipBytes));
        ArchitectureRunDetail? detail = await _runDetailQueryService.GetRunDetailAsync(runId, cancellationToken);
        if (detail is null)
            return null;
        Guid runGuid = Guid.TryParseExact(runId, "N", out Guid g1) ? g1 : Guid.TryParse(runId, out Guid g2) ? g2 : Guid.Empty;
        IReadOnlyList<AuditEvent> audits = runGuid == Guid.Empty ? [] : await _auditRepository.GetFilteredAsync(scope.TenantId, scope.WorkspaceId, scope.ProjectId, new AuditEventFilter { RunId = runGuid, Take = 1000 }, cancellationToken);
        object summary = new
        {
            detail.Run.RunId,
            detail.Run.Status,
            detail.Run.CreatedUtc,
            detail.Run.CompletedUtc,
            ManifestVersion = detail.Run.CurrentManifestVersion,
            detail.IsCommitted,
            TaskCount = detail.Tasks.Count,
            ResultCount = detail.Results.Count,
            DecisionTraceCount = detail.DecisionTraces.Count
        };
        byte[] zipBytes = BuildZipInMemory(summary, audits, detail.DecisionTraces);
        return zipBytes.LongLength > maxZipBytes ? throw new TraceabilityBundleTooLargeException(zipBytes.LongLength, maxZipBytes) : zipBytes;
    }

    private static byte[] BuildZipInMemory(object runSummary, IReadOnlyList<AuditEvent> audits, IReadOnlyList<DecisionTrace> traces)
    {
        using MemoryStream ms = new();
        using (ZipArchive zip = new(ms, ZipArchiveMode.Create, true))
        {
            AddJsonEntry(zip, "run-summary.json", runSummary);
            AddJsonEntry(zip, "audit-events.json", audits);
            AddJsonEntry(zip, "decision-traces.json", traces);
            ZipArchiveEntry readme = zip.CreateEntry("README.txt", CompressionLevel.Fastest);
            using StreamWriter w = new(readme.Open());
            w.WriteLine("ArchLucid traceability bundle — audit slice, decision traces, and run summary.");
            w.WriteLine("LLM full prompts may be omitted per export policy; use admin evidence export when explicitly authorized.");
        }

        return ms.ToArray();
    }

    private static void AddJsonEntry(ZipArchive zip, string path, object payload)
    {
        ZipArchiveEntry entry = zip.CreateEntry(path, CompressionLevel.Fastest);
        using Stream s = entry.Open();
        JsonSerializer.Serialize(s, payload, JsonOptions);
    }
}