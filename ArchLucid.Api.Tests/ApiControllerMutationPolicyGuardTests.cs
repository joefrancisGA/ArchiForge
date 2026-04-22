using System.Reflection;

using ArchLucid.Core.Authorization;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArchLucid.Api.Tests;

/// <summary>
/// Ensures mutating HTTP actions declare a named <see cref="AuthorizeAttribute.Policy"/> (class or method),
/// and that policy names match registered <see cref="ArchLucidPolicies"/> constants plus known extension policies.
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class ApiControllerMutationPolicyGuardTests
{
    [Fact]
    public void All_mutation_actions_have_named_authorization_policy()
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

            if (type.Namespace is null || !type.Namespace.Contains("ArchLucid.Api.Controllers", StringComparison.Ordinal))
            {
                continue;
            }

            violations.AddRange(from method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly) where MethodDeclaresMutationVerb(method) where !MethodOrTypeAllowsAnonymous(type, method) let policy = FirstNonEmptyPolicyOnMethod(method) ?? FirstNonEmptyPolicyOnType(type) where string.IsNullOrWhiteSpace(policy) select $"{type.FullName}.{method.Name} is a mutation without a named [Authorize(Policy = ...)] on the method or controller.");
        }

        violations.Should().BeEmpty();
    }

    [Fact]
    public void All_declared_authorization_policies_are_known_constants_or_registered_aliases()
    {
        HashSet<string> known = BuildKnownPolicyNames();

        Assembly api = typeof(Program).Assembly;
        List<string> unknown = [];

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

            if (type.Namespace is null || !type.Namespace.Contains("ArchLucid.Api.Controllers", StringComparison.Ordinal))
            {
                continue;
            }

            foreach (AuthorizeAttribute attribute in type.GetCustomAttributes<AuthorizeAttribute>(inherit: true))
            {
                CollectUnknownPolicy(attribute.Policy, known, unknown, type.FullName ?? type.Name);
            }

            foreach (MethodInfo method in type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            {
                foreach (AuthorizeAttribute attribute in method.GetCustomAttributes<AuthorizeAttribute>(inherit: false))
                {
                    CollectUnknownPolicy(attribute.Policy, known, unknown, $"{type.FullName}.{method.Name}");
                }
            }
        }

        unknown.Should().BeEmpty();
    }

    private static void CollectUnknownPolicy(string? policy, HashSet<string> known, List<string> unknown, string? context)
    {
        if (string.IsNullOrWhiteSpace(policy))
        {
            return;
        }

        string label = string.IsNullOrWhiteSpace(context) ? "(unknown)" : context;

        if (!known.Contains(policy))
        {
            unknown.Add($"{label}: unknown policy '{policy}'.");
        }
    }

    private static HashSet<string> BuildKnownPolicyNames()
    {
        HashSet<string> known = [];

        foreach (FieldInfo field in typeof(ArchLucidPolicies).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            if (field.FieldType != typeof(string))
            {
                continue;
            }

            object? value = field.GetValue(null);

            if (value is string s && !string.IsNullOrWhiteSpace(s))
            {
                known.Add(s);
            }
        }

        known.Add("CanSeedResults");

        return known;
    }

    private static bool MethodOrTypeAllowsAnonymous(Type controllerType, MethodInfo method)
    {
        if (method.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null)
            return true;

        return controllerType.GetCustomAttribute<AllowAnonymousAttribute>(inherit: true) is not null;
    }

    private static string? FirstNonEmptyPolicyOnMethod(MethodInfo method)
    {
        return (from attribute in method.GetCustomAttributes<AuthorizeAttribute>(inherit: false) where !string.IsNullOrWhiteSpace(attribute.Policy) select attribute.Policy).FirstOrDefault();
    }

    private static string? FirstNonEmptyPolicyOnType(Type controllerType)
    {
        return (from attribute in controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true) where !string.IsNullOrWhiteSpace(attribute.Policy) select attribute.Policy).FirstOrDefault();
    }

    private static bool MethodDeclaresMutationVerb(MethodInfo method)
    {
        return method.GetCustomAttributes(inherit: true).Select(attribute => attribute.GetType().Name).Any(name => name.Equals("HttpPostAttribute", StringComparison.Ordinal) || name.Equals("HttpPutAttribute", StringComparison.Ordinal) || name.Equals("HttpPatchAttribute", StringComparison.Ordinal) || name.Equals("HttpDeleteAttribute", StringComparison.Ordinal));
    }
}
