using System.Reflection;

using ArchLucid.Api.Attributes;
using ArchLucid.Api.Controllers.Admin;
using ArchLucid.Api.Controllers.Pilots;
using ArchLucid.Contracts.Pilots;
using ArchLucid.Core.Tenancy;

using FluentAssertions;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Locks <see cref="RequiresCommercialTenantTierAttribute" /> metadata for Operate / analysis HTTP surfaces (class and
///     selected action gates) so commercial packaging drifts are caught in CI.
/// </summary>
public sealed class CommercialPackagingMetadataTests
{
    [SkippableFact]
    public void Controllers_that_declare_requires_commercial_tier_at_class_level_use_valid_metadata()
    {
        Assembly asm = typeof(RequiresCommercialTenantTierAttribute).Assembly;

        Type controllerBase = typeof(ControllerBase);

        List<Type> gated = asm
            .GetExportedTypes()
            .Where(t =>
                t is { IsClass: true } &&
                controllerBase.IsAssignableFrom(t) &&
                t is { IsAbstract: false, IsNestedPrivate: false } &&
                t.GetCustomAttribute<RequiresCommercialTenantTierAttribute>(inherit: false) is not null)
            .ToList();

        gated.Should().NotBeEmpty(
            "Expected at least one API controller to declare [RequiresCommercialTenantTier] at class scope.");

        foreach (Type t in gated)
        {
            RequiresCommercialTenantTierAttribute? attr =
                t.GetCustomAttribute<RequiresCommercialTenantTierAttribute>(inherit: false);

            attr.Should().NotBeNull($"{t.Name} must carry the attribute directly on the controller type.");
            attr.Arguments.Should().HaveCount(1);

            TenantTier tier = (TenantTier)attr.Arguments[0];
            Assert.True((int)tier > (int)TenantTier.Free, $"{t.FullName} should require Standard or Enterprise.");
        }
    }

    [SkippableFact]
    public void Pilots_post_sponsor_one_pager_declares_standard_commercial_tier()
    {
        MethodInfo? methodCandidate = typeof(PilotsController).GetMethod(
            nameof(PilotsController.PostSponsorOnePager),
            BindingFlags.Instance | BindingFlags.Public);

        MethodInfo method = methodCandidate
                            ?? throw new InvalidOperationException(
                                "Could not resolve PostSponsorOnePager on PilotsController.");

        RequiresCommercialTenantTierAttribute? attr =
            method.GetCustomAttribute<RequiresCommercialTenantTierAttribute>(inherit: false);

        attr.Should().NotBeNull($"{nameof(PilotsController.PostSponsorOnePager)} must stay Standard-gated.");
        attr.Arguments.Should().HaveCount(1);
        attr.Arguments[0].Should().Be(TenantTier.Standard);
    }

    [SkippableFact]
    public void Pilots_get_sponsor_evidence_pack_declares_standard_commercial_tier()
    {
        MethodInfo? methodCandidate = typeof(PilotsController).GetMethod(
            nameof(PilotsController.GetSponsorEvidencePack),
            BindingFlags.Instance | BindingFlags.Public);

        MethodInfo method = methodCandidate
                            ?? throw new InvalidOperationException(
                                "Could not resolve GetSponsorEvidencePack on PilotsController.");

        RequiresCommercialTenantTierAttribute? attr =
            method.GetCustomAttribute<RequiresCommercialTenantTierAttribute>(inherit: false);

        attr.Should().NotBeNull($"{nameof(PilotsController.GetSponsorEvidencePack)} must stay Standard-gated.");
        attr.Arguments.Should().HaveCount(1);
        attr.Arguments[0].Should().Be(TenantTier.Standard);

        ProducesResponseTypeAttribute[]
            produces = method.GetCustomAttributes<ProducesResponseTypeAttribute>().ToArray();

        produces.Should().ContainSingle(a =>
            a.StatusCode == StatusCodes.Status200OK &&
            a.Type == typeof(SponsorEvidencePackResponse));
    }

    [SkippableFact]
    public void Audit_export_audit_declares_enterprise_commercial_tier()
    {
        MethodInfo? methodCandidate = typeof(AuditController).GetMethod(
            nameof(AuditController.ExportAudit),
            BindingFlags.Instance | BindingFlags.Public);

        MethodInfo method = methodCandidate
                            ?? throw new InvalidOperationException("Could not resolve ExportAudit on AuditController.");

        RequiresCommercialTenantTierAttribute? attr =
            method.GetCustomAttribute<RequiresCommercialTenantTierAttribute>(inherit: false);

        attr.Should().NotBeNull($"{nameof(AuditController.ExportAudit)} must stay Enterprise-gated.");
        attr.Arguments.Should().HaveCount(1);
        attr.Arguments[0].Should().Be(TenantTier.Enterprise);
    }
}
