using ArchLucid.AgentRuntime.Prompts;
using ArchLucid.Contracts.Agents;
using ArchLucid.Contracts.Common;
using ArchLucid.Core.Configuration;

using FluentAssertions;

namespace ArchLucid.AgentRuntime.Tests;

[Trait("Category", "Unit")]
public sealed class AgentPromptReproTests
{
    [Fact]
    public void CanonicalHasher_ignores_crlf_vs_lf()
    {
        string a = "line1\nline2";
        string b = "line1\r\nline2";

        AgentPromptCanonicalHasher.Sha256HexUtf8Normalized(a)
            .Should()
            .Be(AgentPromptCanonicalHasher.Sha256HexUtf8Normalized(b));
    }

    [Fact]
    public void CachedCatalog_applies_release_label_from_options()
    {
        AgentPromptCatalogOptions opts = new();
        opts.Versions[AgentTypeKeys.Topology] = "pilot-a";
        IAgentSystemPromptCatalog catalog = AgentPromptCatalogTestFactory.Create(opts);

        ResolvedSystemPrompt r = catalog.Resolve(AgentType.Topology);

        r.TemplateId.Should().Be(TopologySystemPromptTemplate.TemplateId);
        r.TemplateVersion.Should().Be(TopologySystemPromptTemplate.Version);
        r.ReleaseLabel.Should().Be("pilot-a");
        r.ContentSha256Hex.Should().HaveLength(64);
        r.Text.Should().StartWith("You are the ArchLucid Topology Agent.");
    }

    [Fact]
    public void ResolvedSystemPrompt_ToReproMetadata_round_trips()
    {
        ResolvedSystemPrompt r = new("x", "id", "1.0.0", "abc", "lab");

        AgentPromptReproMetadata m = r.ToReproMetadata();

        m.TemplateId.Should().Be("id");
        m.TemplateVersion.Should().Be("1.0.0");
        m.SystemPromptContentSha256Hex.Should().Be("abc");
        m.ReleaseLabel.Should().Be("lab");
    }
}
