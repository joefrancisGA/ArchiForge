using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

public sealed class TryCommandOptionsParseTests
{
    private static readonly object RealAoaiEnvLock = new();

    [Fact]
    public void Parse_WhenReal_setsRealModeAndLongerDefaultCommitDeadline()
    {
        TryCommandOptions? options = TryCommandOptions.Parse(["--real"], out string? error);

        error.Should().BeNull();
        options.Should().NotBeNull();
        options!.RealMode.Should().BeTrue();
        options.StrictReal.Should().BeFalse();
        options.CommitDeadline.Should().Be(TryCommandOptions.RealModeDefaultCommitDeadline);
    }

    [Fact]
    public void Parse_WhenStrictReal_setsStrictReal()
    {
        TryCommandOptions? options = TryCommandOptions.Parse(["--strict-real"], out string? error);

        error.Should().BeNull();
        options.Should().NotBeNull();
        options!.StrictReal.Should().BeTrue();
    }

    [Fact]
    public void Parse_WhenRealAndExplicitCommitDeadline_usesExplicitValue()
    {
        TryCommandOptions? options = TryCommandOptions.Parse(["--real", "--commit-deadline", "90"], out string? error);

        error.Should().BeNull();
        options.Should().NotBeNull();
        options!.CommitDeadline.Should().Be(TimeSpan.FromSeconds(90));
    }

    [Fact]
    public void IsPilotRealAzureOpenAiAttempt_WhenRealButEnvNotOne_returnsFalse()
    {
        lock (RealAoaiEnvLock)
        {
            string? saved = Environment.GetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv);

            try
            {
                Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, "0");

                TryCommandOptions? options = TryCommandOptions.Parse(["--real"], out _);
                options.Should().NotBeNull();
                options!.IsPilotRealAzureOpenAiAttempt.Should().BeFalse();
            }
            finally
            {
                if (saved is null)
                    Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, null);
                else
                    Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, saved);
            }
        }
    }

    [Fact]
    public void IsPilotRealAzureOpenAiAttempt_WhenRealAndEnvOne_returnsTrue()
    {
        lock (RealAoaiEnvLock)
        {
            string? saved = Environment.GetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv);

            try
            {
                Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, "1");

                TryCommandOptions? options = TryCommandOptions.Parse(["--real"], out _);
                options.Should().NotBeNull();
                options!.IsPilotRealAzureOpenAiAttempt.Should().BeTrue();
            }
            finally
            {
                if (saved is null)
                    Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, null);
                else
                    Environment.SetEnvironmentVariable(TryCommandOptions.ArchLucidRealAoaiEnv, saved);
            }
        }
    }
}
