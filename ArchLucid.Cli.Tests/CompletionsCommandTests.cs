using ArchLucid.Cli.Commands;

using FluentAssertions;

namespace ArchLucid.Cli.Tests;

[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class CompletionsCommandTests
{
    [Fact]
    public async Task Bash_emits_complete_function_and_archlucid_word()
    {
        StringWriter outWriter = new();
        StringWriter errWriter = new();
        TextWriter prevOut = Console.Out;
        TextWriter prevErr = Console.Error;
        Console.SetOut(outWriter);
        Console.SetError(errWriter);
        try
        {
            int code = await CompletionsCommand.RunAsync(["bash"]);
            code.Should().Be(0);
            string text = outWriter.ToString();
            text.Should().Contain("complete -F _archlucid_completion archlucid");
            text.Should().Contain("new");
            text.Should().Contain("pilot");
            text.Should().Contain("first-value-report");
            text.Should().Contain("sponsor-one-pager");
            text.Should().Contain("completions");
            text.Should().Contain("roi-bulletin");
            text.Should().Contain("security-trust");
            text.Should().Contain("marketplace");
            text.Should().Contain("manifest");
            text.Should().Contain("golden-cohort");
            text.Should().Contain("procurement-pack");
            text.Should().Contain("reference-evidence");
            text.Should().Contain("proof-pack");
            text.Should().Contain("trial");
            text.Should().Contain("second-run");
            text.Should().Contain("seed-demo-data");
            text.Should().Contain("explain-operator-model");
        }
        finally
        {
            Console.SetOut(prevOut);
            Console.SetError(prevErr);
        }
    }

    [Fact]
    public async Task Powershell_emits_Register_ArgumentCompleter()
    {
        StringWriter outWriter = new();
        TextWriter prevOut = Console.Out;
        Console.SetOut(outWriter);
        try
        {
            int code = await CompletionsCommand.RunAsync(["powershell"]);
            code.Should().Be(0);
            outWriter.ToString().Should().Contain("Register-ArgumentCompleter");
        }
        finally
        {
            Console.SetOut(prevOut);
        }
    }

    [Fact]
    public async Task Unknown_shell_returns_1()
    {
        StringWriter errWriter = new();
        TextWriter prevErr = Console.Error;
        Console.SetError(errWriter);
        try
        {
            int code = await CompletionsCommand.RunAsync(["fish"]);
            code.Should().Be(1);
            errWriter.ToString().Should().Contain("Unknown shell");
        }
        finally
        {
            Console.SetError(prevErr);
        }
    }
}
