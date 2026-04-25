using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;

using JetBrains.Annotations;

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
        public ThreadCommentDto[] Comments { [UsedImplicitly] get; set; } = [];

        public int Status
        {
            [UsedImplicitly]
            get; set;
        }
    }

    private sealed class ThreadCommentDto
    {
        public int ParentCommentId
        {
            [UsedImplicitly]
            get; set;
        }

        public string Content { [UsedImplicitly] get; set; } = string.Empty;

        public int CommentType
        {
            [UsedImplicitly]
            get; set;
        }
    }

    private sealed class StatusCreateDto
    {
        public string State { [UsedImplicitly] get; set; } = string.Empty;

        public string Description { [UsedImplicitly] get; set; } = string.Empty;

        public StatusContextDto Context { [UsedImplicitly] get; set; } = new();

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public string? TargetUrl
        {
            [UsedImplicitly]
            get; set;
        }
    }

    private sealed class StatusContextDto
    {
        public string Name { [UsedImplicitly] get; set; } = string.Empty;

        public string Genre { [UsedImplicitly] get; set; } = string.Empty;
    }
}
