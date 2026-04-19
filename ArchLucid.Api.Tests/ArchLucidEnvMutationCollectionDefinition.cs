namespace ArchLucid.Api.Tests;

/// <summary>
/// Serializes tests that mutate process environment variables (RLS bypass flag) and
/// <see cref="GreenfieldSqlApiFactory"/> boots (same env keys + schema bootstrap <c>SqlRowLevelSecurityBypassAmbient</c>).
/// </summary>
[CollectionDefinition("ArchLucidEnvMutation", DisableParallelization = true)]
public sealed class ArchLucidEnvMutationCollectionDefinition;
