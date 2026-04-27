using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

namespace ArchLucid.Core.GoldenCorpus;

/// <summary>Builds <see cref="ArchitectureRequest"/> payloads for golden-cohort simulator runs.</summary>
public static class GoldenCohortArchitectureRequestFactory
{
    /// <summary>Deterministic request id derived from cohort item id (stable across lock-baseline runs).</summary>
    public static ArchitectureRequest FromCohortItem(GoldenCohortItem item)
    {
        if (item is null)
            throw new ArgumentNullException(nameof(item));

        if (string.IsNullOrWhiteSpace(item.Id))
            throw new ArgumentException("Cohort item id is required.", nameof(item));

        string requestId = $"golden-cohort-{item.Id}".ToLowerInvariant();

        if (requestId.Length > 64)
            requestId = requestId[..64];

        return new ArchitectureRequest
        {
            RequestId = requestId,
            SystemName = $"GoldenCohort_{item.Id}",
            Description =
                $"{item.Title}. Provide a concise target architecture suitable for automated merge tests; prefer Azure patterns.",
            Environment = "prod",
            CloudProvider = CloudProvider.Azure,
            Constraints = ["Golden cohort regression"],
            RequiredCapabilities = ["App Service or Container Apps", "Azure SQL"],
        };
    }
}
