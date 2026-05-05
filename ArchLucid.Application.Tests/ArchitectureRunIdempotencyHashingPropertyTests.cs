using System.Text.Json;

using ArchLucid.Application.Runs;
using ArchLucid.Contracts.Common;
using ArchLucid.Contracts.Requests;

using FluentAssertions;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Application.Tests;

/// <summary>Property-based checks for <see cref="ArchitectureRunIdempotencyHashing"/>.</summary>
[Trait("Category", "Unit")]
[Trait("Suite", "Core")]
public sealed class ArchitectureRunIdempotencyHashingPropertyTests
{
    [Property(Arbitrary = [typeof(ArchitectureRunIdempotencyArbitraries)], MaxTest = 200)]
    public void HashIdempotencyKey_is_deterministic_for_non_whitespace_key(
        ArchitectureRunIdempotencyArbitraries.NonWhitespaceIdempotencyKey key)
    {
        byte[] first = ArchitectureRunIdempotencyHashing.HashIdempotencyKey(key.Value);
        byte[] second = ArchitectureRunIdempotencyHashing.HashIdempotencyKey(key.Value);

        first.Should().Equal(second);
    }

    [Property(Arbitrary = [typeof(ArchitectureRunIdempotencyArbitraries)], MaxTest = 200)]
    public void HashIdempotencyKey_distinct_non_empty_strings_yield_distinct_digests(
        ArchitectureRunIdempotencyArbitraries.DistinctNonEmptyStringPair pair)
    {
        byte[] ha = ArchitectureRunIdempotencyHashing.HashIdempotencyKey(pair.A);
        byte[] hb = ArchitectureRunIdempotencyHashing.HashIdempotencyKey(pair.B);

        ha.Should().NotBeEquivalentTo(hb);
    }

    [Property(Arbitrary = [typeof(ArchitectureRunIdempotencyArbitraries)], MaxTest = 200)]
    public void FingerprintRequest_is_deterministic_across_calls(ArchitectureRequest request)
    {
        byte[] first = ArchitectureRunIdempotencyHashing.FingerprintRequest(request);
        byte[] second = ArchitectureRunIdempotencyHashing.FingerprintRequest(request);

        first.Should().Equal(second);
    }

    [Property(Arbitrary = [typeof(ArchitectureRunIdempotencyArbitraries)], MaxTest = 200)]
    public void FingerprintRequest_changes_when_any_single_field_changes(ArchitectureRequest request)
    {
        byte[] baseline = ArchitectureRunIdempotencyHashing.FingerprintRequest(request);

        foreach (ArchitectureRequest variant in ArchitectureRunIdempotencyArbitraries.SingleFieldVariants(request))
        {
            byte[] mutated = ArchitectureRunIdempotencyHashing.FingerprintRequest(variant);
            mutated.Should().NotBeEquivalentTo(baseline);
        }
    }
}

/// <summary>FsCheck generators for <see cref="ArchitectureRunIdempotencyHashingPropertyTests"/>.</summary>
public static class ArchitectureRunIdempotencyArbitraries
{
    public readonly record struct NonWhitespaceIdempotencyKey(string Value);

    public readonly record struct DistinctNonEmptyStringPair(string A, string B);

    public static Arbitrary<NonWhitespaceIdempotencyKey> NonWhitespaceIdempotencyKeys()
    {
        return Arb.Default.String()
            .Generator
            .Where(static s => !string.IsNullOrWhiteSpace(s))
            .Select(static s => new NonWhitespaceIdempotencyKey(s))
            .ToArbitrary();
    }

    public static Arbitrary<DistinctNonEmptyStringPair> DistinctNonEmptyStringPairs()
    {
        Gen<DistinctNonEmptyStringPair> gen =
            from a in Arb.Default.String().Generator.Where(static s => !string.IsNullOrWhiteSpace(s))
            from suffix in Arb.Default.String().Generator
            let b = a + "\u0001" + suffix
            select new DistinctNonEmptyStringPair(a, b);

        return gen.ToArbitrary();
    }

    public static Arbitrary<ArchitectureRequest> ValidArchitectureRequests()
    {
        Gen<ArchitectureRequest> gen =
            from descSeed in Arb.Default.String().Generator
            from sysSeed in Arb.Default.String().Generator
            from envSeed in Arb.Default.String().Generator
            select BuildArchitectureRequest(descSeed, sysSeed, envSeed);

        return gen.ToArbitrary();
    }

    internal static IEnumerable<ArchitectureRequest> SingleFieldVariants(ArchitectureRequest request)
    {
        yield return Mutate(Clone(request), static r => r.RequestId += "\u03b4");

        yield return Mutate(Clone(request), static r => r.Description += "\u03b4");

        yield return Mutate(Clone(request), static r => r.SystemName += "\u03b4");

        yield return Mutate(Clone(request), static r => r.Environment = r.Environment == "prod" ? "staging" : "prod");

        yield return Mutate(Clone(request), static r => r.Constraints = [.. r.Constraints, "extra-constraint"]);

        yield return Mutate(Clone(request), static r => r.RequiredCapabilities = [.. r.RequiredCapabilities, "cap-x"]);

        yield return Mutate(Clone(request), static r => r.Assumptions = [.. r.Assumptions, "assume-x"]);

        yield return Mutate(
            Clone(request),
            static r => r.PriorManifestVersion = r.PriorManifestVersion is null ? "mv-prop" : null);

        yield return Mutate(Clone(request), static r => r.InlineRequirements = [.. r.InlineRequirements, "req-x"]);

        yield return Mutate(
            Clone(request),
            static r => r.Documents =
            [
                .. r.Documents,
                new ContextDocumentRequest { Name = "doc", ContentType = "text/plain", Content = "x" },
            ]);

        yield return Mutate(Clone(request), static r => r.PolicyReferences = [.. r.PolicyReferences, "pol-x"]);

        yield return Mutate(Clone(request), static r => r.TopologyHints = [.. r.TopologyHints, "topo-x"]);

        yield return Mutate(Clone(request), static r => r.SecurityBaselineHints = [.. r.SecurityBaselineHints, "sec-x"]);

        yield return Mutate(
            Clone(request),
            static r => r.InfrastructureDeclarations =
            [
                .. r.InfrastructureDeclarations,
                new InfrastructureDeclarationRequest { Name = "infra", Format = "json", Content = "{}" },
            ]);
    }

    private static ArchitectureRequest Mutate(ArchitectureRequest clone, Action<ArchitectureRequest> change)
    {
        change(clone);

        return clone;
    }

    private static ArchitectureRequest Clone(ArchitectureRequest r) =>
        JsonSerializer.Deserialize<ArchitectureRequest>(
            JsonSerializer.Serialize(r, ContractJson.Default),
            ContractJson.Default)!;

    private static ArchitectureRequest BuildArchitectureRequest(string descSeed, string sysSeed, string envSeed)
    {
        string description = EnsureMinLength10(descSeed);
        string systemName = EnsureNonEmpty(sysSeed);
        string environment = EnsureNonEmpty(envSeed);

        return new ArchitectureRequest
        {
            RequestId = Guid.NewGuid().ToString("N"),
            Description = description,
            SystemName = systemName,
            Environment = environment,
            CloudProvider = CloudProvider.Azure,
            Constraints = [],
            RequiredCapabilities = [],
            Assumptions = [],
            PriorManifestVersion = null,
            InlineRequirements = [],
            Documents = [],
            PolicyReferences = [],
            TopologyHints = [],
            SecurityBaselineHints = [],
            InfrastructureDeclarations = [],
        };
    }

    private static string EnsureMinLength10(string? seed)
    {
        string s = seed ?? string.Empty;

        const string pad = "0123456789";

        if (s.Length >= 10)
        {
            return s;
        }

        return s + pad[..(10 - s.Length)];
    }

    private static string EnsureNonEmpty(string? seed)
    {
        return string.IsNullOrWhiteSpace(seed) ? "Sys" : seed.Trim();
    }
}
