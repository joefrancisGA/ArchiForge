using ArchLucid.Persistence.Connections;
using ArchLucid.TestSupport;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Verifies <see cref="SqlTransientDetector" /> correctly classifies transient vs permanent exceptions.
/// </summary>
[Trait("Category", "Unit")]
public sealed class SqlTransientDetectorTests
{
    [Theory]
    [InlineData(-2)]
    [InlineData(40613)]
    [InlineData(40197)]
    [InlineData(49918)]
    [InlineData(49919)]
    [InlineData(49920)]
    [InlineData(1205)]
    public void IsTransient_SqlException_WithTransientErrorNumber_ReturnsTrue(int errorNumber)
    {
        SqlException ex = SqlExceptionTestFactory.Create(errorNumber);

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18456)]
    [InlineData(547)]
    public void IsTransient_SqlException_WithNonTransientErrorNumber_ReturnsFalse(int errorNumber)
    {
        SqlException ex = SqlExceptionTestFactory.Create(errorNumber);

        SqlTransientDetector.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_NullSqlException_ReturnsFalse()
    {
        SqlTransientDetector.IsTransient(null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_TimeoutException_ReturnsTrue()
    {
        Exception ex = new TimeoutException("timed out");

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_GenericException_ReturnsFalse()
    {
        Exception ex = new InvalidOperationException("not transient");

        SqlTransientDetector.IsTransient(ex).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_NullException_ReturnsFalse()
    {
        SqlTransientDetector.IsTransient((Exception)null!).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_InnerSqlException_TransientCode_ReturnsTrue()
    {
        SqlException inner = SqlExceptionTestFactory.Create(-2);
        Exception outer = new InvalidOperationException("wrapper", inner);

        SqlTransientDetector.IsTransient(outer).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_InnerSqlException_NonTransientCode_ReturnsFalse()
    {
        SqlException inner = SqlExceptionTestFactory.Create(18456);
        Exception outer = new InvalidOperationException("wrapper", inner);

        SqlTransientDetector.IsTransient(outer).Should().BeFalse();
    }

    [Fact]
    public void IsTransient_DeepNestedSqlException1205_ReturnsTrue()
    {
        SqlException deadlock = SqlExceptionTestFactory.Create(1205);
        Exception mid = new InvalidOperationException("repository", deadlock);
        Exception outer = new InvalidOperationException("unit of work", mid);

        SqlTransientDetector.IsTransient(outer).Should().BeTrue();
    }
}
