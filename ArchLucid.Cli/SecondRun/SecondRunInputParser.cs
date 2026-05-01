using System.Globalization;
using System.Text;
using System.Text.Json;

using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using Tomlyn;
using Tomlyn.Model;

namespace ArchLucid.Cli.SecondRun;

/// <summary>
///     Parses the tiny <c>SECOND_RUN.toml</c> / <c>SECOND_RUN.json</c> schema into <see cref="ArchitectureRequest" />
///     .
/// </summary>
public static class SecondRunInputParser
{
    /// <summary>Hard cap for the on-disk second-run file (UTF-8 bytes) — keeps CLI + accidental paste payloads bounded.</summary>
    public const int MaxUtf8Bytes = 24 * 1024;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true
    };

    /// <summary>Parses UTF-8 text; infers JSON when trimmed content starts with <c>{</c>, otherwise TOML.</summary>
    public static SecondRunParseOutcome ParseFromUtf8(ReadOnlySpan<byte> utf8, string sourceLabel)
    {
        if (utf8.Length > MaxUtf8Bytes)
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.PayloadTooLarge,
                $"Second-run input exceeds maximum size ({MaxUtf8Bytes} UTF-8 bytes) for {sourceLabel}.");
        }

        string text = Encoding.UTF8.GetString(utf8);
        ReadOnlySpan<char> trimmed = text.AsSpan().TrimStart();

        if (trimmed.Length > 0 && trimmed[0] == '{')
            return ParseJson(text, sourceLabel);

        return ParseToml(text, sourceLabel);
    }

    /// <summary>Reads a file and delegates to <see cref="ParseFromUtf8" />.</summary>
    public static SecondRunParseOutcome ParseFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return SecondRunParseOutcome.Fail(SecondRunParseFailureCode.BadRequest, "File path is required.");

        if (!File.Exists(path))
            return SecondRunParseOutcome.Fail(SecondRunParseFailureCode.BadRequest, $"File not found: {path}");

        byte[] bytes = File.ReadAllBytes(path);

        return ParseFromUtf8(bytes, path);
    }

    private static SecondRunParseOutcome ParseJson(string text, string sourceLabel)
    {
        SecondRunWireDto? dto;

        try
        {
            dto = JsonSerializer.Deserialize<SecondRunWireDto>(text, JsonOptions);
        }
        catch (JsonException ex)
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Malformed JSON in {sourceLabel}: {ex.Message}");
        }

        if (dto is null)
            return SecondRunParseOutcome.Fail(SecondRunParseFailureCode.BadRequest,
                $"Empty JSON document in {sourceLabel}.");

        return ValidateAndMap(dto, sourceLabel);
    }

    private static SecondRunParseOutcome ParseToml(string text, string sourceLabel)
    {
        TomlTable model;

        try
        {
            model = Toml.ToModel<TomlTable>(text);
        }
        catch (TomlException ex)
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Malformed TOML in {sourceLabel}: {ex.Message}");
        }

        SecondRunWireDto dto = TomlTableToDto(model);

        return ValidateAndMap(dto, sourceLabel);
    }

    private static SecondRunWireDto TomlTableToDto(TomlTable table)
    {
        SecondRunWireDto dto = new()
        {
            Name = ReadString(table, "name"),
            Description = ReadString(table, "description"),
            Environment = ReadString(table, "environment"),
            CloudProvider = ReadString(table, "cloud_provider"),
            RequestId = ReadString(table, "request_id"),
            Components = ReadStringList(table, "components"),
            DataStores = ReadStringList(table, "data_stores"),
            PublicEndpoints = ReadStringList(table, "public_endpoints"),
            CompliancePosture = ReadStringList(table, "compliance_posture"),
            Constraints = ReadStringList(table, "constraints"),
            Assumptions = ReadStringList(table, "assumptions"),
            InlineRequirements = ReadStringList(table, "inline_requirements")
        };

        return dto;
    }

    private static string? ReadString(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out object value))
            return null;

        return value switch
        {
            string s => s,
            int i => i.ToString(CultureInfo.InvariantCulture),
            long l => l.ToString(CultureInfo.InvariantCulture),
            bool b => b ? "true" : "false",
            _ => Convert.ToString(value, CultureInfo.InvariantCulture)
        };
    }

    private static List<string> ReadStringList(TomlTable table, string key)
    {
        if (!table.TryGetValue(key, out object value))
            return [];

        if (value is TomlArray arr)
        {
            List<string> list = [];

            foreach (object item in arr.OfType<object>())
            {
                string? s = item switch
                {
                    string str => str,
                    int i => i.ToString(CultureInfo.InvariantCulture),
                    long l => l.ToString(CultureInfo.InvariantCulture),
                    _ => Convert.ToString(item, CultureInfo.InvariantCulture)
                };

                if (!string.IsNullOrWhiteSpace(s))
                    list.Add(s.Trim());
            }

            return list;
        }

        string? one = ReadString(table, key);

        return string.IsNullOrWhiteSpace(one) ? [] : [one.Trim()];
    }

    private static SecondRunParseOutcome ValidateAndMap(SecondRunWireDto dto, string sourceLabel)
    {
        string name = (dto.Name ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(name))
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Missing required field 'name' in {sourceLabel}.");
        }

        string description = (dto.Description ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(description))
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Missing required field 'description' in {sourceLabel}.");
        }

        if (description.Length < 10)
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Field 'description' must be at least 10 characters (API contract) in {sourceLabel}.");
        }

        List<string> components = NormalizeList(dto.Components);
        List<string> dataStores = NormalizeList(dto.DataStores);
        List<string> publicEndpoints = NormalizeList(dto.PublicEndpoints);
        List<string> compliance = NormalizeList(dto.CompliancePosture);
        List<string> inline = NormalizeList(dto.InlineRequirements);

        List<string> derivedInline = [];

        foreach (string ds in dataStores)
            derivedInline.Add($"Datastore: {ds}");

        foreach (string ep in publicEndpoints)
            derivedInline.Add($"Public endpoint: {ep}");

        List<string> mergedInline = [.. inline, .. derivedInline];

        string requestId = (dto.RequestId ?? string.Empty).Trim().Replace("-", string.Empty);

        if (string.IsNullOrWhiteSpace(requestId))
            requestId = Guid.NewGuid().ToString("N");

        if (requestId.Length > 64)
        {
            return SecondRunParseOutcome.Fail(
                SecondRunParseFailureCode.BadRequest,
                $"Field 'request_id' exceeds 64 characters in {sourceLabel}.");
        }

        string environment = (dto.Environment ?? "prod").Trim();

        if (string.IsNullOrWhiteSpace(environment))
            environment = "prod";

        ArchitectureRequest request = new()
        {
            RequestId = requestId,
            SystemName = name,
            Description = description,
            Environment = environment,
            CloudProvider = CloudProvider.Azure,
            Constraints = NormalizeList(dto.Constraints),
            RequiredCapabilities = components,
            Assumptions = NormalizeList(dto.Assumptions),
            InlineRequirements = mergedInline,
            SecurityBaselineHints = compliance
        };

        return SecondRunParseOutcome.Ok(request);
    }

    private static List<string> NormalizeList(List<string>? source)
    {
        return source is null
            ? []
            : source
                .Select(static s => s.Trim())
                .Where(static s => !string.IsNullOrWhiteSpace(s))
                .ToList();
    }
}
