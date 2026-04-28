using ArchLucid.Persistence.Connections;

using ArchLucid.TestSupport;

using Microsoft.Data.SqlClient;

namespace ArchLucid.Persistence.Tests.Connections;

[Trait("Category", "Unit")]
public sealed class SqlUniqueConstraintViolationDetectorTests
{
    [Theory]
    [InlineData(2601)]
    [InlineData(2627)]
    public void IsUniqueKeyViolation_ReturnsTrue_WhenTopLevelSqlException(int number)
    {
        SqlException ex = SqlExceptionTestFactory.Create(number);

        SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(ex).Should().BeTrue();
    }

    [Fact]
    public void IsUniqueKeyViolation_ReturnsFalse_ForNonUniqueError()
    {
        SqlException ex = SqlExceptionTestFactory.Create(50000);

        SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(ex).Should().BeFalse();
    }

    [Fact]
    public void IsUniqueKeyViolation_ReturnsTrue_WhenInnerSqlException()
    {
        SqlException inner = SqlExceptionTestFactory.Create(2627);
        Exception ex = new("wrap", inner);

        SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(ex).Should().BeTrue();
    }

    [Fact]
    public void IsUniqueKeyViolation_ReturnsFalse_ForNull()
    {
        SqlUniqueConstraintViolationDetector.IsUniqueKeyViolation(null).Should().BeFalse();
    }
}
