using System.Text;
using System.Text.Json;

using ArchLucid.Api.Models.Tenancy;
using ArchLucid.Api.ProblemDetails;
using ArchLucid.Application.Tenancy;
using ArchLucid.Core.Audit;
using ArchLucid.Core.Diagnostics;
using ArchLucid.Core.Tenancy;

using Asp.Versioning;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ArchLucid.Api.Controllers;

/// <summary>Anonymous tenant self-registration (Free tier).</summary>
[ApiController]
[AllowAnonymous]
[EnableRateLimiting("registration")]
[ApiVersion("1.0")]
[Route("v{version:apiVersion}/register")]
public sealed class RegistrationController(
    ITenantProvisioningService provisioning,
    IAuditService audit,
    ITrialTenantBootstrapService trialBootstrap) : ControllerBase
{
    private const string FriendlyValidation = "The registration could not be completed. Check the organization name, email, and optional review-cycle fields, then try again.";
    private const string FriendlyInternal = "We could not complete your registration. Please try again in a few minutes. If the problem continues, share the correlationId field on this error response (or the X-Correlation-ID response header) with your administrator.";

    private readonly IAuditService _audit = audit ?? throw new ArgumentNullException(nameof(audit));

    private readonly ITenantProvisioningService _provisioning =
        provisioning ?? throw new ArgumentNullException(nameof(provisioning));

    private readonly ITrialTenantBootstrapService _trialBootstrap =
        trialBootstrap ?? throw new ArgumentNullException(nameof(trialBootstrap));

    /// <summary>Creates a Free-tier tenant and default workspace (idempotent by organization slug).</summary>
    [HttpPost]
    [ProducesResponseType(typeof(TenantProvisioningResult), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(TenantProvisioningResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(Microsoft.AspNetCore.Mvc.ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> RegisterAsync(
        [FromBody] TenantRegistrationRequest? body,
        CancellationToken cancellationToken = default)
    {
        if (body is null)
        {
            ArchLucidInstrumentation.RecordTrialRegistrationFailure("validation");

            await _audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialRegistrationFailed,
                    ActorUserId = "anonymous@request",
                    ActorUserName = "anonymous",
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            reason = "validation",
                            code = "body_required",
                            message = (string?)"Request body is required."
                        })
                },
                cancellationToken);

            return this.BadRequestProblem("Request body is required.", ProblemTypes.RequestBodyRequired);
        }

        string? normalizedBaselineSource = NormalizeBaselineReviewCycleSource(body.BaselineReviewCycleSource);
        if (body.BaselineReviewCycleHours is null && normalizedBaselineSource is not null)
        {
            return await RegisterFailureValidationAsync(
                body,
                "validation",
                "BaselineReviewCycleHours is required when BaselineReviewCycleSource is provided.",
                "baseline_incomplete",
                "BaselineReviewCycleHours is required when BaselineReviewCycleSource is provided.",
                cancellationToken);
        }

        if (body.BaselineReviewCycleHours is <= 0m or > 10_000m)
        {
            return await RegisterFailureValidationAsync(
                body,
                "validation",
                "BaselineReviewCycleHours must be greater than 0 and at most 10000.",
                "baseline_out_of_range",
                "Baseline review cycle hours must be between 0 and 10,000 (exclusive of zero).",
                cancellationToken);
        }

        if (body.CompanySize is { } companySize)
        {
            if (!StructuredBaselineConstants.AllowedCompanySizes.Contains(companySize))
            {
                return await RegisterFailureValidationAsync(
                    body,
                    "validation",
                    "CompanySize is not a supported band.",
                    "company_size_invalid",
                    "Company size must be one of the allowed options when provided.",
                    cancellationToken);
            }
        }

        if (body.ArchitectureTeamSize is <= 0 or > 10_000)
        {
            return await RegisterFailureValidationAsync(
                body,
                "validation",
                "ArchitectureTeamSize must be between 1 and 10000 when provided.",
                "architecture_team_size_out_of_range",
                "Architecture team size must be between 1 and 10,000 when provided.",
                cancellationToken);
        }

        if (body.IndustryVertical is { } ind)
        {
            if (!StructuredBaselineConstants.IndustryVerticals.Contains(ind))
            {
                return await RegisterFailureValidationAsync(
                    body,
                    "validation",
                    "IndustryVertical is not in the curated list.",
                    "industry_vertical_invalid",
                    "Industry must be one of the listed options (or Other) when provided.",
                    cancellationToken);
            }
        }

        if (string.Equals(body.IndustryVertical, "Other", StringComparison.Ordinal) &&
            string.IsNullOrWhiteSpace(body.IndustryVerticalOther))
        {
            return await RegisterFailureValidationAsync(
                body,
                "validation",
                "IndustryVerticalOther is required when IndustryVertical is Other.",
                "industry_other_required",
                "Please specify your industry when you select \"Other.\"",
                cancellationToken);
        }

        string actorEmail = body.AdminEmail.Trim();

        await _audit.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TrialSignupAttempted,
                ActorUserId = actorEmail,
                ActorUserName =
                    string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actorEmail : body.AdminDisplayName.Trim(),
                TenantId = Guid.Empty,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                DataJson = JsonSerializer.Serialize(new { channel = "api_register" })
            },
            cancellationToken);

        try
        {
            TenantProvisioningResult result = await _provisioning.ProvisionAsync(
                new TenantProvisioningRequest
                {
                    Name = body.OrganizationName.Trim(),
                    AdminEmail = body.AdminEmail.Trim(),
                    Tier = TenantTier.Free,
                    AuditActorOverride = body.AdminEmail.Trim()
                },
                cancellationToken);

            if (result.WasAlreadyProvisioned)
            {
                ArchLucidInstrumentation.RecordTrialSignupFailure("provision", "duplicate_slug");
                ArchLucidInstrumentation.RecordTrialRegistrationFailure("conflict");

                await _audit.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.TrialRegistrationFailed,
                        ActorUserId = actorEmail,
                        ActorUserName =
                            string.IsNullOrWhiteSpace(body.AdminDisplayName)
                                ? actorEmail
                                : body.AdminDisplayName.Trim(),
                        TenantId = Guid.Empty,
                        WorkspaceId = Guid.Empty,
                        ProjectId = Guid.Empty,
                        DataJson = JsonSerializer.Serialize(new { reason = "conflict", code = "duplicate_slug" })
                    },
                    cancellationToken);

                return this.ConflictProblem(
                    "An organization with this name is already registered.",
                    ProblemTypes.Conflict);
            }

            string actor = body.AdminEmail.Trim();

            await _audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TenantSelfRegistered,
                    ActorUserId = actor,
                    ActorUserName =
                        string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actor : body.AdminDisplayName.Trim(),
                    TenantId = result.TenantId,
                    WorkspaceId = result.DefaultWorkspaceId,
                    ProjectId = result.DefaultProjectId,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            organizationName = body.OrganizationName.Trim(),
                            adminEmail = body.AdminEmail.Trim(),
                            companySize = body.CompanySize,
                            architectureTeamSize = body.ArchitectureTeamSize,
                            industryVertical = body.IndustryVertical,
                            industryVerticalOther = string.Equals(
                                body.IndustryVertical,
                                "Other",
                                StringComparison.Ordinal)
                                ? body.IndustryVerticalOther?.Trim()
                                : null
                        })
                },
                cancellationToken);

            TrialSignupBaselineReviewCycleCapture? baselineCapture = body.BaselineReviewCycleHours is { } h
                ? new TrialSignupBaselineReviewCycleCapture(h, normalizedBaselineSource, DateTimeOffset.UtcNow)
                : null;

            bool hasCompanyProfile = body.CompanySize is not null
                || body.ArchitectureTeamSize is not null
                || !string.IsNullOrWhiteSpace(body.IndustryVertical);

            TrialSignupCompanyProfileCapture? companyProfile = hasCompanyProfile
                ? new TrialSignupCompanyProfileCapture(
                    body.CompanySize,
                    body.ArchitectureTeamSize,
                    body.IndustryVertical,
                    string.Equals(body.IndustryVertical, "Other", StringComparison.Ordinal)
                        ? body.IndustryVerticalOther?.Trim()
                        : null)
                : null;

            await _trialBootstrap.TryBootstrapAfterSelfRegistrationAsync(
                result,
                actor,
                baselineCapture,
                companyProfile,
                cancellationToken);

            if (baselineCapture is not null)
            {
                await _audit.LogAsync(
                    new AuditEvent
                    {
                        EventType = AuditEventTypes.TrialBaselineReviewCycleCaptured,
                        ActorUserId = actor,
                        ActorUserName =
                            string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actor : body.AdminDisplayName.Trim(),
                        TenantId = result.TenantId,
                        WorkspaceId = Guid.Empty,
                        ProjectId = Guid.Empty,
                        DataJson = JsonSerializer.Serialize(
                            new
                            {
                                baselineReviewCycleHours = baselineCapture.Hours,
                                baselineReviewCycleSource = normalizedBaselineSource,
                                capturedUtc = baselineCapture.CapturedUtc,
                                companySize = companyProfile?.CompanySize,
                                architectureTeamSize = companyProfile?.ArchitectureTeamSize,
                                industryVertical = companyProfile?.IndustryVertical,
                                industryVerticalOther = companyProfile?.IndustryVerticalOther
                            })
                    },
                    cancellationToken);
            }
            else
                ArchLucidInstrumentation.RecordTrialSignupBaselineSkipped();

            ArchLucidInstrumentation.RecordOperatorTaskSuccess("first_session_completed");

            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (Exception ex) when (ex is ArgumentException or InvalidOperationException)
        {
            ArchLucidInstrumentation.RecordTrialSignupFailure("validation", ex.GetType().Name);
            ArchLucidInstrumentation.RecordTrialRegistrationFailure("validation");

            await _audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialRegistrationFailed,
                    ActorUserId = actorEmail,
                    ActorUserName = string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actorEmail : body.AdminDisplayName.Trim(),
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty,
                    DataJson = JsonSerializer.Serialize(new
                    {
                        reason = "validation",
                        code = "exception",
                        type = ex.GetType().Name,
                        message = ex.Message
                    })
                },
                cancellationToken);

            return this.BadRequestProblem(FriendlyValidation, ProblemTypes.ValidationFailed);
        }
        catch (Exception ex)
        {
            ArchLucidInstrumentation.RecordTrialSignupFailure("server", ex.GetType().Name);
            ArchLucidInstrumentation.RecordTrialRegistrationFailure("internal");

            await _audit.LogAsync(
                new AuditEvent
                {
                    EventType = AuditEventTypes.TrialRegistrationFailed,
                    ActorUserId = actorEmail,
                    ActorUserName = string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actorEmail : body.AdminDisplayName.Trim(),
                    TenantId = Guid.Empty,
                    WorkspaceId = Guid.Empty,
                    ProjectId = Guid.Empty,
                    DataJson = JsonSerializer.Serialize(
                        new
                        {
                            reason = "internal",
                            type = ex.GetType().Name,
                            message = ex.Message
                        })
                },
                cancellationToken);

            if (ex is not OperationCanceledException)
                return this.InternalServerErrorProblem(FriendlyInternal);

            throw;
        }
    }

    private async Task<IActionResult> RegisterFailureValidationAsync(
        TenantRegistrationRequest body,
        string reasonLabel,
        string logMessage,
        string code,
        string userMessage,
        CancellationToken cancellationToken)
    {
        ArchLucidInstrumentation.RecordTrialRegistrationFailure("validation");

        string actor = string.IsNullOrWhiteSpace(body.AdminEmail) ? "anonymous@request" : body.AdminEmail.Trim();
        string name = string.IsNullOrWhiteSpace(body.AdminDisplayName) ? actor : body.AdminDisplayName.Trim();

        await _audit.LogAsync(
            new AuditEvent
            {
                EventType = AuditEventTypes.TrialRegistrationFailed,
                ActorUserId = actor,
                ActorUserName = name,
                TenantId = Guid.Empty,
                WorkspaceId = Guid.Empty,
                ProjectId = Guid.Empty,
                DataJson = JsonSerializer.Serialize(new
                {
                    reason = reasonLabel,
                    code,
                    message = logMessage
                })
            },
            cancellationToken);

        return this.BadRequestProblem(userMessage, ProblemTypes.ValidationFailed);
    }

    private static string? NormalizeBaselineReviewCycleSource(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
            return null;

        string trimmed = raw.Trim();
        StringBuilder builder = new(trimmed.Length);

        foreach (char c in trimmed.Where(c => !char.IsControl(c)))
            builder.Append(c);

        if (builder.Length == 0)
            return null;

        string s = builder.ToString();

        return s.Length > 256 ? s[..256] : s;
    }
}
