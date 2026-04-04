using ArchiForge.Api.Auth.Models;
using ArchiForge.Api.Services.Admin;
using ArchiForge.Persistence.Data.Repositories;
using ArchiForge.Host.Core.Configuration;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement;

namespace ArchiForge.Api.Controllers;

/// <summary>Operator diagnostics (outbox depth, leader leases, feature flags).</summary>
[ApiController]
[Authorize(Policy = ArchiForgePolicies.AdminAuthority)]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/admin")]
public sealed class AdminController(
    IAdminDiagnosticsService diagnostics,
    IFeatureManager featureManager) : ControllerBase
{
    private readonly IAdminDiagnosticsService _diagnostics =
        diagnostics ?? throw new ArgumentNullException(nameof(diagnostics));

    private readonly IFeatureManager _featureManager =
        featureManager ?? throw new ArgumentNullException(nameof(featureManager));

    /// <summary>Pending asynchronous authority and retrieval indexing work.</summary>
    [HttpGet("diagnostics/outboxes")]
    [ProducesResponseType(typeof(AdminOutboxSnapshot), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOutboxes(CancellationToken cancellationToken = default)
    {
        AdminOutboxSnapshot snapshot = await _diagnostics.GetOutboxSnapshotAsync(cancellationToken);

        return Ok(snapshot);
    }

    /// <summary>SQL host leader lease holders (empty when InMemory storage or election disabled).</summary>
    [HttpGet("diagnostics/leases")]
    [ProducesResponseType(typeof(IReadOnlyList<HostLeaderLeaseSnapshot>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetLeases(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<HostLeaderLeaseSnapshot> rows =
            await _diagnostics.GetLeasesAsync(cancellationToken);

        return Ok(rows);
    }

    /// <summary>Effective state of the async authority pipeline feature flag.</summary>
    [HttpGet("features/async-authority-pipeline")]
    [ProducesResponseType(typeof(AsyncAuthorityPipelineFeatureState), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAsyncAuthorityPipelineFeature(CancellationToken cancellationToken = default)
    {
        bool enabled =
            await _featureManager.IsEnabledAsync(AuthorityPipelineFeatureFlags.AsyncAuthorityPipeline, cancellationToken);

        return Ok(new AsyncAuthorityPipelineFeatureState(enabled));
    }
}

/// <summary>JSON body for <c>GET .../features/async-authority-pipeline</c>.</summary>
public sealed record AsyncAuthorityPipelineFeatureState(bool Enabled);
