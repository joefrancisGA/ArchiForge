using System.IO.Compression;
using System.Net;

using ArchLucid.Application.Support;

using FluentAssertions;

namespace ArchLucid.Api.Tests;

/// <summary>
///     HTTP coverage for <c>POST /v1/admin/support-bundle</c> — the in-product
///     support-bundle download (PENDING_QUESTIONS.md item 37, owner decisions F + G,
///     2026-04-23). Asserts the policy guard (
///     <see cref="ArchLucid.Core.Authorization.ArchLucidPolicies.ExecuteAuthority" />)
///     and that the happy path returns a non-empty ZIP with the expected entries.
/// </summary>
[Trait("Category", "Integration")]
[Trait("Suite", "Core")]
public sealed class SupportBundleEndpointTests
{
    private const string EndpointPath = "/v1/admin/support-bundle";

    [Fact]
    public async Task Post_WithReaderRole_Returns403_BecauseExecuteAuthorityIsRequired()
    {
        await using ReaderRoleArchLucidApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using HttpResponseMessage response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden,
            "owner decision F gates /admin/support-bundle on ExecuteAuthority; Reader role lacks it.");
    }

    [Fact]
    public async Task Post_WithApiKeyAuthEnabledButNoKey_Returns401()
    {
        await using HealthEndpointSecurityApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using HttpResponseMessage response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized,
            "an unauthenticated caller must be rejected before policy evaluation.");
    }

    [Fact]
    public async Task Post_WithDevelopmentBypassDefaultRole_ReturnsZipBundle()
    {
        await using ArchLucidApiFactory factory = new();
        using HttpClient client = factory.CreateClient();
        IntegrationTestBase.WireDefaultSqlIntegrationScopeHeaders(client);

        using HttpResponseMessage response = await client.PostAsync(EndpointPath, null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.Should().Be("application/zip");

        byte[] zipBytes = await response.Content.ReadAsByteArrayAsync();
        zipBytes.Should().NotBeEmpty();

        IReadOnlyList<string> entryNames = ListZipEntryNames(zipBytes);
        entryNames.Should().Contain(SupportBundleAssembler.ReadmeFileName);
        entryNames.Should().Contain(SupportBundleAssembler.ManifestFileName);
        entryNames.Should().Contain(SupportBundleAssembler.BuildFileName);
        entryNames.Should().Contain(SupportBundleAssembler.EnvironmentFileName);
        entryNames.Should().Contain(SupportBundleAssembler.ReferencesFileName);

        string? contentDisposition = response.Content.Headers.ContentDisposition?.ToString();
        contentDisposition.Should().NotBeNullOrEmpty();
        contentDisposition.Should().Contain("archlucid-support-bundle-",
            "the controller derives the download file name from the assembled artifact.");
    }

    private static IReadOnlyList<string> ListZipEntryNames(byte[] zipBytes)
    {
        using MemoryStream memory = new(zipBytes);
        using ZipArchive archive = new(memory, ZipArchiveMode.Read);

        return [.. archive.Entries.Select(e => e.FullName)];
    }
}
