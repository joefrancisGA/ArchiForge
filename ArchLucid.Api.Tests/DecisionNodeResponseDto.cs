using ArchLucid.Contracts.Decisions;

namespace ArchLucid.Api.Tests;

public sealed class DecisionNodeResponseDto
{
    public List<DecisionNode> Decisions
    {
        get;
        set;
    } = [];
}
