using System.Security.Claims;
using System.Text.Encodings.Web;

using ArchLucid.Api.Auth.Services;
using ArchLucid.Api.Authentication;
using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using Moq;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Unit tests for <see cref="ApiKeyAuthenticationHandler" /> (handler is non-sealed to allow this test double).
/// </summary>
public sealed class ApiKeyAuthenticationHandlerTests
{
    [Fact]
    public async Task When_enabled_false_and_bypass_false_returns_failure()
    {
        DefaultHttpContext http = new();
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "false",
                ["Authentication:ApiKey:DevelopmentBypassAll"] = "false"
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure.Should().NotBeNull();
    }

    [Fact]
    public async Task When_enabled_true_and_valid_admin_key_returns_success_with_admin_role()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Append("X-Api-Key", "secret-admin");
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "true", ["Authentication:ApiKey:AdminKey"] = "secret-admin"
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal?.FindFirst(ClaimTypes.Name)?.Value.Should().Be("ApiKeyAdmin");
        result.Principal?.IsInRole(ArchLucidRoles.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task When_enabled_true_and_comma_separated_admin_keys_either_segment_authenticates()
    {
        DefaultHttpContext httpFirst = new();
        httpFirst.Request.Headers.Append("X-Api-Key", "new-admin");
        DefaultHttpContext httpSecond = new();
        httpSecond.Request.Headers.Append("X-Api-Key", "old-admin");
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        IReadOnlyDictionary<string, string?> cfg = new Dictionary<string, string?>
        {
            ["Authentication:ApiKey:Enabled"] = "true", ["Authentication:ApiKey:AdminKey"] = "new-admin, old-admin"
        };

        AuthenticateResult first = await CreateHandler(cfg, httpFirst, env).InvokeHandleAuthenticateAsync();
        AuthenticateResult second = await CreateHandler(cfg, httpSecond, env).InvokeHandleAuthenticateAsync();

        first.Succeeded.Should().BeTrue();
        second.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task When_enabled_true_and_comma_separated_admin_keys_with_empty_segment_ignores_blanks()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Append("X-Api-Key", "only-key");
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "true", ["Authentication:ApiKey:AdminKey"] = "  only-key  , , "
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeTrue();
    }

    [Fact]
    public async Task When_enabled_true_and_invalid_key_returns_failure()
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Append("X-Api-Key", "wrong");
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "true", ["Authentication:ApiKey:AdminKey"] = "good-key"
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure?.Message.Should().Contain("Invalid");
    }

    [Fact]
    public async Task When_enabled_false_and_bypass_true_in_development_returns_success_without_header()
    {
        DefaultHttpContext http = new();
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "false", ["Authentication:ApiKey:DevelopmentBypassAll"] = "true"
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeTrue();
        result.Principal?.Identity?.Name.Should().Be("DevUser");
        result.Principal?.IsInRole(ArchLucidRoles.Admin).Should().BeTrue();
    }

    [Fact]
    public async Task When_enabled_false_and_bypass_true_in_production_returns_failure()
    {
        DefaultHttpContext http = new();
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Production);
        ApiKeyAuthHandlerTestDouble handler = CreateHandler(
            new Dictionary<string, string?>
            {
                ["Authentication:ApiKey:Enabled"] = "false", ["Authentication:ApiKey:DevelopmentBypassAll"] = "true"
            },
            http,
            env);

        AuthenticateResult result = await handler.InvokeHandleAuthenticateAsync();

        result.Succeeded.Should().BeFalse();
        result.Failure?.Message.Should().Contain("Production");
    }

    /// <summary>
    ///     Simulates configuration reload: first request sees key A, subsequent
    ///     <see cref="IOptionsMonitor{TOptions}.CurrentValue" /> sees key B.
    /// </summary>
    [Fact]
    public async Task When_api_key_options_monitor_advances_old_material_fails_and_new_succeeds()
    {
        IHostEnvironment env = Mock.Of<IHostEnvironment>(e => e.EnvironmentName == Environments.Development);
        ApiKeyAuthenticationOptions first = new() { Enabled = true, AdminKey = "rotate-a" };
        ApiKeyAuthenticationOptions second = new() { Enabled = true, AdminKey = "rotate-b" };
        int pass = 0;
        Mock<IOptionsMonitor<ApiKeyAuthenticationOptions>> apiKeyMonitor = new();
        apiKeyMonitor.Setup(m => m.CurrentValue).Returns(() => Interlocked.Increment(ref pass) == 1 ? first : second);

        DefaultHttpContext httpOkOld = new();
        httpOkOld.Request.Headers.Append("X-Api-Key", "rotate-a");
        ApiKeyAuthHandlerTestDouble h1 = CreateHandlerWithApiKeyMonitor(apiKeyMonitor.Object, httpOkOld, env);
        (await h1.InvokeHandleAuthenticateAsync()).Succeeded.Should().BeTrue();

        DefaultHttpContext httpFailOld = new();
        httpFailOld.Request.Headers.Append("X-Api-Key", "rotate-a");
        ApiKeyAuthHandlerTestDouble h2 = CreateHandlerWithApiKeyMonitor(apiKeyMonitor.Object, httpFailOld, env);
        (await h2.InvokeHandleAuthenticateAsync()).Succeeded.Should().BeFalse();

        DefaultHttpContext httpOkNew = new();
        httpOkNew.Request.Headers.Append("X-Api-Key", "rotate-b");
        ApiKeyAuthHandlerTestDouble h3 = CreateHandlerWithApiKeyMonitor(apiKeyMonitor.Object, httpOkNew, env);
        (await h3.InvokeHandleAuthenticateAsync()).Succeeded.Should().BeTrue();
    }

    private static ApiKeyAuthHandlerTestDouble CreateHandler(
        IReadOnlyDictionary<string, string?> configData,
        HttpContext httpContext,
        IHostEnvironment environment)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(configData).Build();
        ServiceCollection services = [];
        services.AddOptions();
        services.Configure<ApiKeyAuthenticationOptions>(
            configuration.GetSection(ApiKeyAuthenticationOptions.SectionPath));
        using ServiceProvider sp = services.BuildServiceProvider();
        IOptionsMonitor<ApiKeyAuthenticationOptions> apiKeyMonitor =
            sp.GetRequiredService<IOptionsMonitor<ApiKeyAuthenticationOptions>>();

        return CreateHandlerWithApiKeyMonitor(apiKeyMonitor, httpContext, environment);
    }

    private static ApiKeyAuthHandlerTestDouble CreateHandlerWithApiKeyMonitor(
        IOptionsMonitor<ApiKeyAuthenticationOptions> apiKeyMonitor,
        HttpContext httpContext,
        IHostEnvironment environment)
    {
        Mock<IOptionsMonitor<AuthenticationSchemeOptions>> monitor = new();
        AuthenticationSchemeOptions schemeOptions = new();
        monitor.Setup(m => m.CurrentValue).Returns(schemeOptions);
        monitor.Setup(m => m.Get(It.IsAny<string>())).Returns(schemeOptions);

        ApiKeyAuthHandlerTestDouble handler = new(
            monitor.Object,
            NullLoggerFactory.Instance,
            UrlEncoder.Default,
            apiKeyMonitor,
            environment);

        AuthenticationScheme scheme = new(
            AuthServiceCollectionExtensions.ApiKeySchemeName,
            "API Key",
            typeof(ApiKeyAuthenticationHandler));

        handler.InitializeAsync(scheme, httpContext).GetAwaiter().GetResult();
        return handler;
    }

    private sealed class ApiKeyAuthHandlerTestDouble : ApiKeyAuthenticationHandler
    {
        public ApiKeyAuthHandlerTestDouble(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory loggerFactory,
            UrlEncoder encoder,
            IOptionsMonitor<ApiKeyAuthenticationOptions> apiKeyOptions,
            IHostEnvironment environment)
            : base(options, loggerFactory, encoder, apiKeyOptions, environment)
        {
        }

        public Task<AuthenticateResult> InvokeHandleAuthenticateAsync()
        {
            return HandleAuthenticateAsync();
        }
    }
}
