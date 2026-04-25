using ArchLucid.Contracts.Governance;

using FsCheck;
using FsCheck.Xunit;

namespace ArchLucid.Contracts.Tests.Governance;

/// <summary>
///     FsCheck properties for <see cref="GovernanceEnvironmentOrder" /> (promotion ladder invariants).
/// </summary>
[Trait("Suite", "Core")]
[Trait("Category", "Unit")]
public sealed class GovernanceEnvironmentOrderPropertyTests
{
    private static readonly string[] KnownEnvironments =
    [
        GovernanceEnvironment.Dev,
        GovernanceEnvironment.Test,
        GovernanceEnvironment.Prod
    ];

    [Property(MaxTest = 300)]
    public Property Valid_single_step_matches_known_ladder()
    {
        Gen<(string Source, string Target)> gen =
            Gen.Elements(
                (GovernanceEnvironment.Dev, GovernanceEnvironment.Test),
                (GovernanceEnvironment.Test, GovernanceEnvironment.Prod),
                ("dev", "test"),
                ("TEST", "prod"));

        return Prop.ForAll(
            Arb.From(gen),
            pair => GovernanceEnvironmentOrder.IsValidPromotion(pair.Source, pair.Target));
    }

    [Property(MaxTest = 300)]
    public Property Invalid_when_same_environment()
    {
        Gen<string> envGen = Gen.Elements(KnownEnvironments);

        return Prop.ForAll(
            Arb.From(envGen),
            env => !GovernanceEnvironmentOrder.IsValidPromotion(env, env));
    }

    [Property(MaxTest = 300)]
    public Property Invalid_when_skip_or_reverse_on_known_triple()
    {
        return Prop.ForAll(
            Arb.From(
                Gen.Elements(
                    (GovernanceEnvironment.Dev, GovernanceEnvironment.Prod),
                    (GovernanceEnvironment.Test, GovernanceEnvironment.Dev),
                    (GovernanceEnvironment.Prod, GovernanceEnvironment.Test),
                    (GovernanceEnvironment.Dev, GovernanceEnvironment.Dev))),
            pair => !GovernanceEnvironmentOrder.IsValidPromotion(pair.Item1, pair.Item2));
    }

    /// <summary>
    ///     Whenever promotion is accepted, it must be exactly dev→test or test→prod (case-insensitive).
    /// </summary>
    [Property(MaxTest = 400)]
    public Property When_valid_then_must_be_single_ladder_step()
    {
        return Prop.ForAll(
            Arb.From(Arb.Default.String().Generator),
            Arb.From(Arb.Default.String().Generator),
            (a, b) =>
            {
                if (!GovernanceEnvironmentOrder.IsValidPromotion(a, b))
                {
                    return true;
                }

                bool devTest = string.Equals(a, GovernanceEnvironment.Dev, StringComparison.OrdinalIgnoreCase)
                               && string.Equals(b, GovernanceEnvironment.Test, StringComparison.OrdinalIgnoreCase);

                bool testProd = string.Equals(a, GovernanceEnvironment.Test, StringComparison.OrdinalIgnoreCase)
                                && string.Equals(b, GovernanceEnvironment.Prod, StringComparison.OrdinalIgnoreCase);

                return devTest || testProd;
            });
    }
}
