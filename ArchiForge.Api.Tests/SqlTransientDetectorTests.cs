using System.Reflection;

using ArchiForge.Persistence.Connections;

using FluentAssertions;

using Microsoft.Data.SqlClient;

namespace ArchiForge.Api.Tests;

/// <summary>
/// Verifies <see cref="SqlTransientDetector"/> correctly classifies transient vs permanent exceptions.
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
    public void IsTransient_SqlException_WithTransientErrorNumber_ReturnsTrue(int errorNumber)
    {
        SqlException ex = CreateSqlException(errorNumber);

        SqlTransientDetector.IsTransient(ex).Should().BeTrue();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(18456)]
    [InlineData(547)]
    public void IsTransient_SqlException_WithNonTransientErrorNumber_ReturnsFalse(int errorNumber)
    {
        SqlException ex = CreateSqlException(errorNumber);

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
        SqlException inner = CreateSqlException(-2);
        Exception outer = new InvalidOperationException("wrapper", inner);

        SqlTransientDetector.IsTransient(outer).Should().BeTrue();
    }

    [Fact]
    public void IsTransient_InnerSqlException_NonTransientCode_ReturnsFalse()
    {
        SqlException inner = CreateSqlException(18456);
        Exception outer = new InvalidOperationException("wrapper", inner);

        SqlTransientDetector.IsTransient(outer).Should().BeFalse();
    }

    /// <summary>
    /// Creates a <see cref="SqlException"/> with the given error number via reflection,
    /// since <see cref="SqlException"/> has no public constructor.
    /// </summary>
    private static SqlException CreateSqlException(int number)
    {
        SqlErrorCollection errors = (SqlErrorCollection)Activator.CreateInstance(
            typeof(SqlErrorCollection),
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args: null,
            culture: null)!;

        // Microsoft.Data.SqlClient 6.x: internal ctor is
        // (int infoNumber, byte state, byte errorClass, string server, string message, string procedure,
        //  int lineNumber, int nativeError, Exception exception) — older builds used uint for native/win32.
        SqlError error = (SqlError)Activator.CreateInstance(
            typeof(SqlError),
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args:
            [
                number,
                (byte)0,
                (byte)0,
                "server",
                "message",
                "procedure",
                0,
                0,
                null
            ],
            culture: null)!;

        typeof(SqlErrorCollection)
            .GetMethod("Add", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(errors, [error]);

        SqlException ex = (SqlException)Activator.CreateInstance(
            typeof(SqlException),
            BindingFlags.NonPublic | BindingFlags.Instance,
            binder: null,
            args:
            [
                "Test SQL exception",   // message
                errors,                 // errorCollection
                null,                   // innerException
                Guid.Empty              // conId
            ],
            culture: null)!;

        return ex;
    }
}
