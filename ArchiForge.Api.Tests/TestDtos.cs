namespace ArchiForge.Api.Tests;

public sealed class CreateRunResponseDto
{
    public RunDto Run { get; set; } = new();
    public EvidenceBundleDto EvidenceBundle { get; set; } = new();
    public List<AgentTaskDto> Tasks { get; set; } = [];
}

public sealed class RunDto
{
    public string RunId { get; set; } = string.Empty;
    public string RequestId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? CurrentManifestVersion { get; set; }
}

public sealed class EvidenceBundleDto
{
    public string EvidenceBundleId { get; set; } = string.Empty;
}

public sealed class AgentTaskDto
{
    public string TaskId { get; set; } = string.Empty;
    public string AgentType { get; set; } = string.Empty;
    public string Objective { get; set; } = string.Empty;
}

public sealed class GetRunResponseDto
{
    public RunDto Run { get; set; } = new();
    public List<AgentTaskDto> Tasks { get; set; } = [];
    public List<object> Results { get; set; } = [];
}

public sealed class SeedFakeResultsResponseDto
{
    public string Message { get; set; } = string.Empty;
    public string RunId { get; set; } = string.Empty;
    public int ResultCount { get; set; }
}

public sealed class ExecuteRunResponseDto
{
    public string RunId { get; set; } = string.Empty;
    public List<object> Results { get; set; } = [];
}

public sealed class CommitRunResponseDto
{
    public ManifestDto Manifest { get; set; } = new();
    public List<DecisionTraceDto> DecisionTraces { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

public sealed class ManifestDto
{
    public string RunId { get; set; } = string.Empty;
    public string SystemName { get; set; } = string.Empty;
    public List<ManifestServiceDto> Services { get; set; } = [];
    public List<ManifestDatastoreDto> Datastores { get; set; } = [];
    public ManifestGovernanceDto Governance { get; set; } = new();
    public ManifestMetadataDto Metadata { get; set; } = new();
}

public sealed class ManifestServiceDto
{
    public string ServiceId { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public List<string> RequiredControls { get; set; } = [];
}

public sealed class ManifestDatastoreDto
{
    public string DatastoreId { get; set; } = string.Empty;
    public string DatastoreName { get; set; } = string.Empty;
    public bool PrivateEndpointRequired { get; set; }
}

public sealed class ManifestGovernanceDto
{
    public List<string> RequiredControls { get; set; } = [];
    public List<string> ComplianceTags { get; set; } = [];
}

public sealed class ManifestMetadataDto
{
    public string ManifestVersion { get; set; } = string.Empty;
    public string? ParentManifestVersion { get; set; }
    public List<string> DecisionTraceIds { get; set; } = [];
}

public sealed class DecisionTraceDto
{
    public string TraceId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventDescription { get; set; } = string.Empty;
}

public sealed class ExpectedManifestSummary
{
    public string SystemName { get; set; } = string.Empty;

    public List<string> Services { get; set; } = [];

    public List<string> Datastores { get; set; } = [];

    public List<string> RequiredControls { get; set; } = [];
}