using System.Text.Json;

using ArchLucid.Api.Models.Auth;
using ArchLucid.Api.Auth.Services;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Identity;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Configuration;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Scoping;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;

namespace ArchLucid.Api.Controllers.Auth;

/// <summary>Local email/password trial identity (gated by <c>Auth:Trial:Modes</c> including <c>LocalIdentity</c>).</summary>
[ApiController]
[AllowAnonymous]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/auth/trial/local")]
public sealed class TrialLocalIdentityAuthController(
    IOptions<TrialAuthOptions> trialOptions,
    ITrialLocalIdentityService identity,
    ILocalTrialJwtIssuer jwtIssuer,
    IAuditService auditService) : ControllerBase
{
    private readonly IOptions<TrialAuthOptions> _trialOptions =
        trialOptions ?? throw new ArgumentNullException(nameof(trialOptions));

    private readonly ITrialLocalIdentityService _identity =
        identity ?? throw new ArgumentNullException(nameof(identity));

    private readonly ILocalTrialJwtIssuer _jwtIssuer = jwtIssuer ?? throw new ArgumentNullException(nameof(jwtIssuer));

    private readonly IAuditService _auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

    /// <summary>Registers a pending user; email must be verified before trial provisioning when LocalIdentity is enabled.</summary>
    [HttpPost("register")]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(TrialLocalRegisterResponse), StatusCodes.Status201Created)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] TrialLocalRegisterRequest? body,
        CancellationToken cancellationToken)
    {
        if (!IsLocalIdentityEnabled())
            return this.NotFoundProblem(
                "Trial local identity is not enabled for this environment.",
                ProblemTypes.ResourceNotFound);

        if (body?.Email is null || body.Password is null)
            return this.BadRequestProblem("Email and password are required.", ProblemTypes.ValidationFailed);

        string email = body.Email.Trim();

        await _auditService.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TrialSignupAttempted,
                ActorUserId = email,
                ActorUserName = email,
                TenantId = Guid.Empty,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                DataJson = JsonSerializer.Serialize(new { channel = "trial_local_register" }),
            },
            cancellationToken);

        try
        {
            TrialLocalRegistrationResult created =
                await _identity.RegisterAsync(body.Email, body.Password, cancellationToken);

            ArchLucidInstrumentation.RecordTrialSignup("trial_local", "local_identity_register");

            return StatusCode(
                StatusCodes.Status201Created,
                new TrialLocalRegisterResponse { UserId = created.UserId, VerificationToken = created.VerificationToken });
        }
        catch (ArgumentException ex)
        {
            ArchLucidInstrumentation.RecordTrialSignupFailure("trial_local_register", ex.GetType().Name);

            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialSignupFailed,
                    ActorUserId = email,
                    ActorUserName = email,
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty,
                    DataJson = JsonSerializer.Serialize(new { stage = "trial_local_register", reason = ex.GetType().Name }),
                },
                cancellationToken);

            return this.BadRequestProblem(ex.Message, ProblemTypes.ValidationFailed);
        }
        catch (InvalidOperationException ex)
        {
            ArchLucidInstrumentation.RecordTrialSignupFailure("trial_local_register", ex.GetType().Name);

            await _auditService.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialSignupFailed,
                    ActorUserId = email,
                    ActorUserName = email,
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty,
                    DataJson = JsonSerializer.Serialize(new { stage = "trial_local_register", reason = ex.GetType().Name }),
                },
                cancellationToken);

            return this.ConflictProblem(ex.Message, ProblemTypes.Conflict);
        }
    }

    /// <summary>Confirms email ownership using the token returned from <see cref="RegisterAsync"/>.</summary>
    [HttpPost("verify-email")]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> VerifyEmailAsync(
        [FromBody] TrialLocalVerifyEmailRequest? body,
        CancellationToken cancellationToken)
    {
        if (!IsLocalIdentityEnabled())
            return this.NotFoundProblem(
                "Trial local identity is not enabled for this environment.",
                ProblemTypes.ResourceNotFound);

        if (body?.Email is null || body.Token is null)
            return this.BadRequestProblem("Email and token are required.", ProblemTypes.ValidationFailed);

        bool ok = await _identity.VerifyEmailAsync(body.Email, body.Token, cancellationToken);

        if (!ok)
            return this.BadRequestProblem("Invalid or expired verification token.", ProblemTypes.ValidationFailed);

        return NoContent();
    }

    /// <summary>Issues a JWT suitable for <c>ArchLucidAuth:JwtSigningPublicKeyPemPath</c> validation (Reader role by default).</summary>
    [HttpPost("token")]
    [EnableRateLimiting("registration")]
    [ProducesResponseType(typeof(TrialLocalTokenResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> TokenAsync(
        [FromBody] TrialLocalTokenRequest? body,
        CancellationToken cancellationToken)
    {
        if (!IsLocalIdentityEnabled())
            return this.NotFoundProblem(
                "Trial local identity is not enabled for this environment.",
                ProblemTypes.ResourceNotFound);

        if (body?.Email is null || body.Password is null)
            return this.BadRequestProblem("Email and password are required.", ProblemTypes.ValidationFailed);

        TrialLocalAuthResult? auth = await _identity.AuthenticateAsync(body.Email, body.Password, cancellationToken);

        if (auth is null)
            return Unauthorized();

        Guid tenantId = body.TenantId ?? ScopeIds.DefaultTenant;
        Guid workspaceId = body.WorkspaceId ?? ScopeIds.DefaultWorkspace;
        Guid projectId = body.ProjectId ?? ScopeIds.DefaultProject;

        TrialLocalIdentityOptions local = _trialOptions.Value.LocalIdentity;
        int lifetimeSeconds = Math.Clamp(local.AccessTokenLifetimeMinutes, 5, 24 * 60) * 60;

        string jwt = _jwtIssuer.IssueAccessToken(auth.UserId, auth.Email, auth.Role, tenantId, workspaceId, projectId);

        return Ok(
            new TrialLocalTokenResponse
            {
                AccessToken = jwt,
                TokenType = "Bearer",
                ExpiresInSeconds = lifetimeSeconds,
            });
    }

    private bool IsLocalIdentityEnabled() =>
        TrialAuthModeConstants.HasMode(_trialOptions.Value.Modes, TrialAuthModeConstants.LocalIdentity);
}
