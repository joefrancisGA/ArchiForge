using ArchiForge.ContextIngestion.Topology;

using FluentAssertions;

namespace ArchiForge.ContextIngestion.Tests;

[Trait("Category", "Unit")]
public sealed class TopologyHintStableObjectIdsTests
{
    [Fact]
    public void FromHintName_IsDeterministic()
    {
        string a = TopologyHintStableObjectIds.FromHintName("hub-vnet");
        string b = TopologyHintStableObjectIds.FromHintName("hub-vnet");

        a.Should().Be(b);
        a.Should().HaveLength(32);
    }

    [Fact]
    public void FromHintName_DifferentHints_Differ()
    {
        string a = TopologyHintStableObjectIds.FromHintName("a");
        string b = TopologyHintStableObjectIds.FromHintName("b");

        a.Should().NotBe(b);
    }
}
