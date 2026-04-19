using Microsoft.AspNetCore.Authorization;

namespace ArchLucid.Host.Core.Authorization;

/// <summary>
/// Authorization requirement satisfied when the tenant in <see cref="ArchLucid.Core.Scoping.ScopeContext"/> is
/// allowed to perform mutating operations (not expired / over run or seat limits for an active trial).
/// </summary>
public sealed class TrialActiveRequirement : IAuthorizationRequirement;
