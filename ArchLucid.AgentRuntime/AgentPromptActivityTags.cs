using System.Diagnostics;

using ArchLucid.AgentRuntime.Prompts;

namespace ArchLucid.AgentRuntime;

/// <summary>Maps resolved prompt metadata onto the current <see cref="Activity"/> (agent handler span).</summary>
public static class AgentPromptActivityTags
{
    /// <summary>Sets low-cardinality tags for dashboards; release label is omitted when not configured.</summary>
    public static void Apply(ResolvedSystemPrompt resolved)
    {
        ArgumentNullException.ThrowIfNull(resolved);
        Activity? activity = Activity.Current;

        if (activity is null)
        {
            return;
        }

        activity.SetTag("archiforge.agent.prompt_template_id", resolved.TemplateId);
        activity.SetTag("archiforge.agent.prompt_template_version", resolved.TemplateVersion);
        activity.SetTag("archiforge.agent.prompt_content_sha256", resolved.ContentSha256Hex);

        if (!string.IsNullOrWhiteSpace(resolved.ReleaseLabel))
        {
            activity.SetTag("archiforge.agent.prompt_release_label", resolved.ReleaseLabel);
        }
    }
}
