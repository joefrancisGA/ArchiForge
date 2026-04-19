using System.Text.Json;

namespace ArchLucid.Api.Models.Evolution;

/// <summary>Extracts structured shadow metrics from persisted <see cref="ArchLucid.Contracts.Evolution.EvolutionSimulationRunRecord.OutcomeJson"/>.</summary>
internal static class EvolutionOutcomeShadowReader
{
    private static readonly JsonSerializerOptions DeserializeOptions = new(JsonSerializerDefaults.Web);

    /// <summary>
    /// <paramref name="shadowKind"/> is <c>60R-v2</c>, <c>legacy</c>, <c>none</c>, <c>invalid</c>, or <c>unparsed</c>.
    /// </summary>
    internal static void TryReadShadow(string outcomeJson, out EvolutionShadowOutcomeSnapshot? shadow, out string shadowKind)
    {
        shadow = null;
        shadowKind = "none";

        if (string.IsNullOrWhiteSpace(outcomeJson))
            return;


        try
        {
            using JsonDocument doc = JsonDocument.Parse(outcomeJson);
            JsonElement root = doc.RootElement;

            if (root.TryGetProperty("schemaVersion", out JsonElement sv) &&
                string.Equals(sv.GetString(), EvolutionOutcomeParser.SchemaV2, StringComparison.Ordinal) &&
                root.TryGetProperty("shadow", out JsonElement sh) &&
                sh.ValueKind == JsonValueKind.Object)
            {
                shadow = JsonSerializer.Deserialize<EvolutionShadowOutcomeSnapshot>(sh.GetRawText(), DeserializeOptions);
                shadowKind = EvolutionOutcomeParser.SchemaV2;
                return;
            }

            EvolutionShadowOutcomeSnapshot? legacy =
                JsonSerializer.Deserialize<EvolutionShadowOutcomeSnapshot>(outcomeJson, DeserializeOptions);

            if (legacy is not null && !string.IsNullOrWhiteSpace(legacy.ArchitectureRunId))
            {
                shadow = legacy;
                shadowKind = "legacy";
                return;
            }

            shadowKind = "unparsed";
        }
        catch (JsonException)
        {
            shadowKind = "invalid";
        }
    }
}
