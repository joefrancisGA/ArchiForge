using ArchLucid.Persistence.Connections;

using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Connections;

[Trait("Category", "Unit")]
public sealed class SqlTransientDetectorTests
{
    [Theory]
    [InlineData(-2)]
    [InlineData(1205)]
    [InlineData(40613)]
    [InlineData(40197)]
    [InlineData(49918)]
    [InlineData(49919)]
    [InlineData(49920)]
    public void IsTransient_SqlException_ReturnsTrue_ForKnownNumbers(int number)
    {
        SqlException ex = SqlExceptionTestFactory.Create(number);

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [SkippableFact]
    public void IsTransient_SqlException_ReturnsFalse_ForOtherNumber()
    {
        SqlException ex = SqlExceptionTestFactory.Create(50000);

        SqlTransientDetector.IsTransient(ex).Should().BeFalse();
    }

    [SkippableFact]
    public void IsTransient_SqlExceptionOverload_ReturnsFalse_ForNull()
    {
        SqlTransientDetector.IsTransient(null).Should().BeFalse();
    }

    [SkippableFact]
    public void IsTransient_Exception_WalksInner_AndDetectsTimeout()
    {
        Exception ex = new("outer", new TimeoutException());

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [SkippableFact]
    public void IsTransient_Exception_WalksInner_AndDetectsWrappedSqlTransient()
    {
        SqlException inner = SqlExceptionTestFactory.Create(1205);
        Exception ex = new("outer", inner);

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [SkippableFact]
    public void IsTransient_Exception_ReturnsFalse_ForNull()
    {
        SqlTransientDetector.IsTransient((Exception?)null).Should().BeFalse();
    }

    [SkippableFact]
    public void IsTransient_Exception_ReturnsFalse_ForNonTransientChain()
    {
        SqlTransientDetector.IsTransient(new InvalidOperationException("outer", new FormatException())).Should().BeFalse();
    }
}
