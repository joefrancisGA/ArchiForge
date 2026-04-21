using Xunit;

namespace ArchLucid.Integrations.AzureDevOps.Tests;

/// <summary>
/// Golden JSON under <c>tests/fixtures/azure-devops-pipeline-task/</c> must match
/// <see cref="AzureDevOpsPullRequestWireFormat"/> and the pipeline-side Node serializers (ADR 0024).
/// </summary>
public sealed class AzureDevOpsRequestBodyParityWithPipelineTaskTests
{
    private static string Fixture(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "tests", "fixtures", "azure-devops-pipeline-task", name));

    [Fact]
    public void SerializeThreadCreate_matches_pipeline_task_golden_fixture()
    {
        string markdown = "## ArchLucid sample markdown\n\n- line\n";
        string actual = AzureDevOpsPullRequestWireFormat.SerializeThreadCreate(markdown);
        string expected = Fixture("thread-create.sample.json").Trim();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SerializeStatusCreate_matches_pipeline_task_golden_fixture_with_target_url()
    {
        string actual = AzureDevOpsPullRequestWireFormat.SerializeStatusCreate(
            "Operator compare ready.",
            "https://operator.example/compare?left=a&right=b");

        string expected = Fixture("status-create.sample.json").Trim();

        Assert.Equal(expected, actual);
    }

    [Fact]
    public void SerializeStatusCreate_matches_pipeline_task_golden_fixture_without_target_url()
    {
        string actual = AzureDevOpsPullRequestWireFormat.SerializeStatusCreate("Short desc", null);
        string expected = Fixture("status-create.no-target-url.sample.json").Trim();

        Assert.Equal(expected, actual);
    }
}
