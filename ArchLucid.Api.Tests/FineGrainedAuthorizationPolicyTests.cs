using System.Net;
using System.Text;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Verifies <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.CanCommitRuns" /> when the
///     <c>commit:run</c> permission claim is absent (Operator role otherwise).
/// </summary>
public sealed class CommitRunRequiresCommitPermissionTests : IClassFixture<OperatorWithoutCommitRunPermissionApiFactory>
{
    private readonly HttpClient _client;

    public CommitRunRequiresCommitPermissionTests(OperatorWithoutCommitRunPermissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CommitRun_returns_403_when_commit_run_permission_claim_missing()
    {
        HttpResponseMessage response = await _client.PostAsync(
            $"/v1/architecture/run/{Guid.NewGuid():D}/commit",
            null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

/// <summary>
///     Verifies <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.CanExportConsultingDocx" /> on analysis
///     consulting DOCX routes.
/// </summary>
public sealed class
    ConsultingDocxRequiresExportPermissionTests : IClassFixture<OperatorWithoutConsultingDocxPermissionApiFactory>
{
    private readonly HttpClient _client;

    public ConsultingDocxRequiresExportPermissionTests(OperatorWithoutConsultingDocxPermissionApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task DownloadConsultingDocx_returns_403_when_export_consulting_docx_claim_missing()
    {
        using StringContent body = new("{}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PostAsync(
            $"/v1/architecture/run/{Guid.NewGuid():D}/analysis-report/export/docx/consulting",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task DownloadConsultingDocxAsync_returns_403_when_export_consulting_docx_claim_missing()
    {
        using StringContent body = new("{}", Encoding.UTF8, "application/json");
        HttpResponseMessage response = await _client.PostAsync(
            $"/v1/architecture/run/{Guid.NewGuid():D}/analysis-report/export/docx/consulting/async",
            body);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}

/// <summary>
///     Verifies <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.CanExportConsultingDocx" /> on architecture
///     package DOCX (Reader satisfies read policy only).
/// </summary>
public sealed class ArchitecturePackageDocxRequiresExportPermissionTests : IClassFixture<ReaderRoleArchLucidApiFactory>
{
    private readonly HttpClient _client;

    public ArchitecturePackageDocxRequiresExportPermissionTests(ReaderRoleArchLucidApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ExportRunDocx_returns_403_when_export_consulting_docx_claim_missing()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"/v1/docx/runs/{Guid.Empty:D}/architecture-package");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
