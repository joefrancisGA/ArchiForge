using System.Reflection;

using Asp.Versioning;

using FluentAssertions;

using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures every MVC controller declares explicit API versioning metadata (URL <c>v1</c> routing depends on it).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiControllerApiVersionMetadataTests
{
    [Fact]
    public void All_controllers_declare_ApiVersion_or_ApiVersionNeutral()
    {
        Assembly apiAssembly = typeof(Program).Assembly;
        IEnumerable<Type> controllers = apiAssembly.GetExportedTypes()
            .Where(static t =>
                t is { IsClass: true, IsAbstract: false }
                && typeof(ControllerBase).IsAssignableFrom(t)
                && t.Name.EndsWith("Controller", StringComparison.Ordinal));

        foreach (Type controller in controllers)
        {
            bool hasNeutral = controller.GetCustomAttributes(true).OfType<ApiVersionNeutralAttribute>().Any();
            bool hasVersion = controller.GetCustomAttributes(true).OfType<ApiVersionAttribute>().Any();

            (hasNeutral || hasVersion).Should().BeTrue(
                "controller {0} must declare [ApiVersion(\"1.0\")] or [ApiVersionNeutral] so Asp.Versioning can route OpenAPI and clients consistently",
                controller.FullName);
        }
    }
}
