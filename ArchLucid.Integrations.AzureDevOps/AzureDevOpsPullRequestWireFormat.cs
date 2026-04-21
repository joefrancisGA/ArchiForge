using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArchLucid.Integrations.AzureDevOps;

/// <summary>
/// Canonical JSON bodies for Azure DevOps Git PR threads and statuses (REST 7.1), shared by
/// <see cref="AzureDevOpsPullRequestDecorator"/> and the pipeline-side Node task — parity tests keep bytes aligned.
/// </summary>
public static class AzureDevOpsPullRequestWireFormat
{
    /// <summary>Shared serializer settings for PR thread/status REST bodies (camelCase, omit null).</summary>
    public static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>Serializes the POST <c>…/pullrequests/{id}/threads</c> body (single text comment, active thread).</summary>
    public static string SerializeThreadCreate(string? markdown)
    {
        ThreadCreateDto body = new()
        {
            Comments =
            [
                new ThreadCommentDto
                {
                    ParentCommentId = 0,
                    Content = markdown ?? string.Empty,
                    CommentType = 1,
                },
            ],
            Status = 1,
        };

        return JsonSerializer.Serialize(body, JsonSerializerOptions);
    }

    /// <summary>Serializes the POST <c>…/pullrequests/{id}/statuses</c> body.</summary>
    public static string SerializeStatusCreate(string? description, string? targetUrl)
    {
        string desc = description ?? string.Empty;

        if (desc.Length > 512)
            desc = desc[..512];

        StatusCreateDto body = new()
        {
            State = "succeeded",
            Description = desc,
            Context = new StatusContextDto { Name = "archlucid-manifest", Genre = "archlucid" },
            TargetUrl = string.IsNullOrWhiteSpace(targetUrl) ? null : targetUrl.Trim(),
        };

        return JsonSerializer.Serialize(body, JsonSerializerOptions);
    }

    private sealed class ThreadCreateDto
    {
        public ThreadCommentDto[] Comments { get; set; } = [];

        public int Status
        {
            get; set;
        }
    }

    private sealed class ThreadCommentDto
    {
        public int ParentCommentId
        {
            get; set;
        }

        public string Content { get; set; } = string.Empty;

        public int CommentType
        {
            get; set;
        }
    }

    private sealed class StatusCreateDto
    {
        public string State { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        public StatusContextDto Context { get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetUrl
        {
            get; set;
        }
    }

    private sealed class StatusContextDto
    {
        public string Name { get; set; } = string.Empty;

        public string Genre { get; set; } = string.Empty;
    }
}
