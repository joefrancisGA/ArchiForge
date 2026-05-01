using ArchLucid.Persistence.Utilities;

namespace ArchLucid.Persistence.Tests.Utilities;

[Trait("Category", "Unit")]
public sealed class DapperRowExpectTests
{
    [SkippableFact]
    public void Required_returns_row_when_not_null()
    {
        object row = new();

        object actual = DapperRowExpect.Required(row, "missing");

        actual.Should().BeSameAs(row);
    }

    [SkippableFact]
    public void Required_throws_with_message_when_null()
    {
        Action act = () => DapperRowExpect.Required<object>(null, "expected row");

        act.Should().Throw<InvalidOperationException>().WithMessage("expected row");
    }
}
