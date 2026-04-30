using ArchLucid.Contracts.Requests;

using FluentAssertions;

using ArchLucid.Core.Requests;

namespace ArchLucid.Core.Tests.Requests;

[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class RequestConstraintClassifierTests
{
    [Fact]
    public void HasManagedIdentityConstraint_returns_true_when_constraint_mentions_managed_identity()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["Use managed identity for Key Vault"]);

        RequestConstraintClassifier.HasManagedIdentityConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void HasManagedIdentityConstraint_is_case_insensitive()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["Managed Identity"]);

        RequestConstraintClassifier.HasManagedIdentityConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void HasPrivateNetworkingConstraint_detects_private_endpoint_phrasing()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["traffic via private endpoint only"]);

        RequestConstraintClassifier.HasPrivateNetworkingConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void HasPrivateNetworkingConstraint_detects_private_networking_phrasing()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["require private networking"]);

        RequestConstraintClassifier.HasPrivateNetworkingConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void HasPrivateNetworkingConstraint_matches_generic_private_word()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["everything must remain private"]);

        RequestConstraintClassifier.HasPrivateNetworkingConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void HasEncryptionConstraint_returns_true_when_encryption_is_mentioned()
    {
        ArchitectureRequest request = CreateRequest(constraints: ["encryption at rest required"]);

        RequestConstraintClassifier.HasEncryptionConstraint(request).Should().BeTrue();
    }

    [Fact]
    public void RequiresSearchCapability_returns_true_when_search_is_required()
    {
        ArchitectureRequest request = CreateRequest(capabilities: ["Hybrid search"]);

        RequestConstraintClassifier.RequiresSearchCapability(request).Should().BeTrue();
    }

    [Fact]
    public void RequiresAiCapability_returns_true_for_openai_phrasing()
    {
        ArchitectureRequest request = CreateRequest(capabilities: ["calls Azure OpenAI"]);

        RequestConstraintClassifier.RequiresAiCapability(request).Should().BeTrue();
    }

    [Fact]
    public void RequiresAiCapability_returns_true_for_generic_ai_capability()
    {
        ArchitectureRequest request = CreateRequest(capabilities: ["embedding models via ai"]);

        RequestConstraintClassifier.RequiresAiCapability(request).Should().BeTrue();
    }

    [Fact]
    public void RequiresSqlCapability_returns_true_when_sql_capability_is_required()
    {
        ArchitectureRequest request = CreateRequest(capabilities: ["relational store (sql)"]);

        RequestConstraintClassifier.RequiresSqlCapability(request).Should().BeTrue();
    }

    [Fact]
    public void All_members_throw_when_request_is_null()
    {
        Action act1 = () => RequestConstraintClassifier.HasManagedIdentityConstraint(null!);

        Action act2 = () => RequestConstraintClassifier.HasPrivateNetworkingConstraint(null!);

        Action act3 = () => RequestConstraintClassifier.HasEncryptionConstraint(null!);

        Action act4 = () => RequestConstraintClassifier.RequiresSearchCapability(null!);

        Action act5 = () => RequestConstraintClassifier.RequiresAiCapability(null!);

        Action act6 = () => RequestConstraintClassifier.RequiresSqlCapability(null!);

        act1.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");

        act2.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");

        act3.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");

        act4.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");

        act5.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");

        act6.Should().ThrowExactly<ArgumentNullException>().Which.ParamName.Should().Be("request");
    }

    private static ArchitectureRequest CreateRequest(List<string>? constraints = null,
        List<string>? capabilities = null)
    {
        return new ArchitectureRequest
        {
            Description = "architecture request for tests",
            SystemName = "TestSystem",
            Environment = "dev",
            Constraints = constraints ?? [],
            RequiredCapabilities = capabilities ?? [],
        };
    }
}
