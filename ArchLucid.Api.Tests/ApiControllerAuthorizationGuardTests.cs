using System.Reflection;

using ArchLucid.Api.Controllers.Admin;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

/// <summary>
///     Ensures API controllers either opt into <see cref="AuthorizeAttribute" /> at class level or per action,
///     or explicitly use <see cref="AllowAnonymousAttribute" /> for intentional public surfaces.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiControllerAuthorizationGuardTests
{
    private static readonly HashSet<string> AllowAnonymousControllerNames =
    [
        nameof(VersionController),
        nameof(DocsController)
    ];

    [Fact]
    public void All_api_controllers_declare_authorization_or_allow_anonymous()
    {
        Assembly api = typeof(Program).Assembly;
        List<string> violations = [];

        foreach (Type type in api.GetTypes())
        {
            if (!type.IsClass || type.IsAbstract || !type.IsPublic)
            {
                continue;
            }

            if (!typeof(ControllerBase).IsAssignableFrom(type))
            {
                continue;
            }

            if (type.Namespace is null ||
                !type.Namespace.Contains("ArchLucid.Api.Controllers", StringComparison.Ordinal))
            {
                continue;
            }

            if (AllowAnonymousControllerNames.Contains(type.Name))
            {
                continue;
            }

            bool classAllowAnonymous = type.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null;
            bool classAuthorize = type.GetCustomAttribute<AuthorizeAttribute>(true) is not null;

            if (classAllowAnonymous || classAuthorize)
            {
                continue;
            }

            violations.AddRange(
                from method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
                where MethodDeclaresHttpVerb(method)
                let actionAuthorize = method.GetCustomAttribute<AuthorizeAttribute>(true) is not null
                let actionAllowAnonymous = method.GetCustomAttribute<AllowAnonymousAttribute>(true) is not null
                where !actionAuthorize && !actionAllowAnonymous
                select $"{type.FullName}.{method.Name} is missing [Authorize] or [AllowAnonymous].");
        }

        violations.Should().BeEmpty();
    }

    /// <summary>
    ///     Detects MVC HTTP verb attributes without referencing <c>HttpMethodAttribute</c> types
    ///     (test project compiles against a slim MVC surface).
    /// </summary>
    private static bool MethodDeclaresHttpVerb(MethodInfo method)
    {
        foreach (object attribute in method.GetCustomAttributes(true))
        {
            string name = attribute.GetType().Name;

            if (name.Equals("AcceptVerbsAttribute", StringComparison.Ordinal))
            {
                return true;
            }

            if (name.StartsWith("Http", StringComparison.Ordinal) &&
                name.EndsWith("Attribute", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
