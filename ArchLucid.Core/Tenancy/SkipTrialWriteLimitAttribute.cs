namespace ArchLucid.Core.Tenancy;

/// <summary>
/// When applied to an MVC action, the trial write gate skips enforcement so billing handoff and similar
/// unblock paths remain reachable while the trial row still shows <c>Active</c>.
/// </summary>
[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public sealed class SkipTrialWriteLimitAttribute : Attribute;
