using System.Reflection;

using ArchLucid.Host.Core.Startup;

using FluentAssertions;

using Microsoft.Extensions.Configuration;

using OpenTelemetry;
using OpenTelemetry.Trace;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Tests for <see cref="ObservabilityTraceSamplingConfigurator" />.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ObservabilityTraceSamplingConfiguratorTests
{
    [Fact]
    public void ConfigureTraceSampling_When_ratio_below_one_registers_parent_based_trace_id_ratio_sampler()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Observability:Tracing:SamplingRatio"] = "0.1" }).Build();

        TracerProviderBuilder builder = Sdk.CreateTracerProviderBuilder();
        ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling(builder, configuration);

        using TracerProvider provider = builder.Build();
        Sampler sampler = GetTracerProviderSampler(provider);

        sampler.Should().BeOfType<ParentBasedSampler>();
        GetRootSampler((ParentBasedSampler)sampler).Should().BeOfType<TraceIdRatioBasedSampler>();
    }

    [Fact]
    public void ConfigureTraceSampling_When_ratio_one_matches_default_built_in_sampler_shape()
    {
        IConfiguration ratioOne = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Observability:Tracing:SamplingRatio"] = "1" }).Build();

        IConfiguration empty = new ConfigurationBuilder().AddInMemoryCollection().Build();

        TracerProviderBuilder configuredBuilder = Sdk.CreateTracerProviderBuilder();
        ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling(configuredBuilder, ratioOne);

        TracerProviderBuilder emptyBuilder = Sdk.CreateTracerProviderBuilder();
        ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling(emptyBuilder, empty);

        using TracerProvider configuredProvider = configuredBuilder.Build();
        using TracerProvider emptyProvider = emptyBuilder.Build();

        Sampler configuredSampler = GetTracerProviderSampler(configuredProvider);
        Sampler emptySampler = GetTracerProviderSampler(emptyProvider);

        configuredSampler.GetType().Should().Be(emptySampler.GetType());
        GetRootSampler((ParentBasedSampler)configuredSampler).Should().BeOfType<AlwaysOnSampler>();
        GetRootSampler((ParentBasedSampler)emptySampler).Should().BeOfType<AlwaysOnSampler>();
    }

    [Fact]
    public void ConfigureTraceSampling_When_ratio_invalid_falls_back_to_full_sampling()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Observability:Tracing:SamplingRatio"] = "not-a-number" }).Build();

        TracerProviderBuilder builder = Sdk.CreateTracerProviderBuilder();
        ObservabilityTraceSamplingConfigurator.ConfigureTraceSampling(builder, configuration);

        using TracerProvider provider = builder.Build();
        Sampler sampler = GetTracerProviderSampler(provider);

        GetRootSampler((ParentBasedSampler)sampler).Should().BeOfType<AlwaysOnSampler>();
    }

    private static Sampler GetTracerProviderSampler(TracerProvider provider)
    {
        PropertyInfo? property = provider.GetType().GetProperty(
            "Sampler",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        property.Should().NotBeNull();

        object? value = property.GetValue(provider);

        value.Should().BeAssignableTo<Sampler>();

        return (Sampler)value;
    }

    private static Sampler GetRootSampler(ParentBasedSampler parentBasedSampler)
    {
        FieldInfo? rootField = typeof(ParentBasedSampler).GetField(
            "rootSampler",
            BindingFlags.Instance | BindingFlags.NonPublic);

        rootField.Should().NotBeNull();

        object? root = rootField.GetValue(parentBasedSampler);

        root.Should().BeAssignableTo<Sampler>();

        return (Sampler)root;
    }
}
