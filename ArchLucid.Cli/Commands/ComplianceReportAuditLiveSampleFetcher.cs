using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Cli.Commands;

/// <summary>Fetches one page of audit events for compliance narrative (non-fatal on failure).</summary>
internal static class ComplianceReportAuditLiveSampleFetcher
{
    private static readonly JsonSerializerOptions Json = new()
    {
        PropertyNameCaseInsensitive = true
    };

    internal static async Task<ComplianceReportAuditLiveSample> TryFetchAsync(
        HttpClient http,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(http);

        try
        {
            int take = ArchLucid.Core.Pagination.PaginationDefaults.MaxListingTake;
            using HttpResponseMessage response =
                await http.GetAsync($"v1/audit?take={take}", cancellationToken);

            if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)

                return new ComplianceReportAuditLiveSample(
                    false,
                    $"HTTP {(int)response.StatusCode} — supply `ARCHLUCID_API_KEY` or bearer token with ReadAuthority for the target scope.",
                    0,
                    new Dictionary<string, int>(),
                    null,
                    null);

            if (!response.IsSuccessStatusCode)
            {
                string body = await response.Content.ReadAsStringAsync(cancellationToken);

                return new ComplianceReportAuditLiveSample(
                    false,
                    $"HTTP {(int)response.StatusCode}: {Truncate(body, 240)}",
                    0,
                    new Dictionary<string, int>(),
                    null,
                    null);
            }

            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            AuditPageDto? page = JsonSerializer.Deserialize<AuditPageDto>(json, Json);

            if (page?.Items is null || page.Items.Count == 0)
            {
                return new ComplianceReportAuditLiveSample(
                    true,
                    null,
                    0,
                    new Dictionary<string, int>(),
                    null,
                    null);
            }

            Dictionary<string, int> counts = new(StringComparer.Ordinal);

            foreach (AuditItemDto item in page.Items)
            {
                if (string.IsNullOrWhiteSpace(item.EventType))
                    continue;

                counts.TryGetValue(item.EventType, out int n);
                counts[item.EventType] = n + 1;
            }

            DateTime? min = page.Items.Min(i => (DateTime?)i.OccurredUtc);
            DateTime? max = page.Items.Max(i => (DateTime?)i.OccurredUtc);

            return new ComplianceReportAuditLiveSample(true, null, page.Items.Count, counts, min, max);
        }
        catch (Exception ex)
        {
            return new ComplianceReportAuditLiveSample(
                false,
                $"{ex.GetType().Name}: {ex.Message}",
                0,
                new Dictionary<string, int>(),
                null,
                null);
        }
    }

    private static string Truncate(string value, int max)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= max)
            return value;

        return value[..max] + "…";
    }

    private sealed class AuditPageDto
    {
        [JsonPropertyName("items")]
        public List<AuditItemDto>? Items
        {
            get;
            set;
        }
    }

    private sealed class AuditItemDto
    {
        [JsonPropertyName("eventType")]
        public string? EventType
        {
            get;
            set;
        }

        [JsonPropertyName("occurredUtc")]
        public DateTime OccurredUtc
        {
            get;
            set;
        }
    }
}
