using System.Text.Json;
using System.Text.Json.Serialization;

using ArchLucid.Core.Diagnostics;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ArchLucid.AgentRuntime.Evaluation.ReferenceCases;

/// <summary>Loads <see cref="AgentOutputReferenceCaseDefinition"/> from a JSON file (lazy, thread-safe).</summary>
public sealed class AgentOutputReferenceCaseCatalog : IAgentOutputReferenceCaseCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly IOptionsMonitor<AgentExecutionReferenceEvaluationOptions> _options;

    private readonly string _contentRootPath;

    private readonly ILogger<AgentOutputReferenceCaseCatalog> _logger;

    private readonly Lock _loadGate = new();

    private IReadOnlyList<AgentOutputReferenceCaseDefinition>? _cached;

    private volatile bool _loadAttempted;

    public AgentOutputReferenceCaseCatalog(
        IOptionsMonitor<AgentExecutionReferenceEvaluationOptions> options,
        string contentRootPath,
        ILogger<AgentOutputReferenceCaseCatalog> logger)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _contentRootPath = contentRootPath ?? throw new ArgumentNullException(nameof(contentRootPath));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public IReadOnlyList<AgentOutputReferenceCaseDefinition> Cases
    {
        get
        {
            if (_loadAttempted && _cached is not null)
            {
                return _cached;
            }

            lock (_loadGate)
            {
                if (_cached is not null)
                {
                    return _cached;
                }

                _cached = LoadCasesLocked();
                _loadAttempted = true;

                return _cached;
            }
        }
    }

    private IReadOnlyList<AgentOutputReferenceCaseDefinition> LoadCasesLocked()
    {
        AgentExecutionReferenceEvaluationOptions opts = _options.CurrentValue;

        if (!opts.Enabled)
        {
            return [];
        }

        if (string.IsNullOrWhiteSpace(opts.ReferenceCasesPath))
        {
            _logger.LogWarning(
                "AgentExecution:ReferenceEvaluation:Enabled is true but ReferenceCasesPath is empty; no reference cases loaded.");

            return [];
        }

        string path = Path.IsPathRooted(opts.ReferenceCasesPath)
            ? opts.ReferenceCasesPath
            : Path.GetFullPath(Path.Combine(_contentRootPath, opts.ReferenceCasesPath.TrimStart('/', '\\')));

        if (!File.Exists(path))
        {
            _logger.LogWarning(
                "Reference cases file not found at {Path}; no reference cases loaded.",
                LogSanitizer.Sanitize(path));

            return [];
        }

        try
        {
            string json = File.ReadAllText(path);
            List<AgentOutputReferenceCaseDefinition>? list =
                JsonSerializer.Deserialize<List<AgentOutputReferenceCaseDefinition>>(json, JsonOptions);

            if (list is null || list.Count == 0)
            {
                return [];
            }

            List<AgentOutputReferenceCaseDefinition> valid = [];

            foreach (AgentOutputReferenceCaseDefinition item in list)
            {
                if (string.IsNullOrWhiteSpace(item.CaseId))
                {
                    _logger.LogWarning(
                        "Skipping reference case with empty CaseId in {Path}.",
                        LogSanitizer.Sanitize(path));

                    continue;
                }

                valid.Add(item);
            }

            _logger.LogInformation(
                "Loaded {Count} agent output reference case(s) from {Path}.",
                valid.Count,
                LogSanitizer.Sanitize(path));

            return valid;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to deserialize reference cases from {Path}; no cases loaded.",
                LogSanitizer.Sanitize(path));

            return [];
        }
    }
}
