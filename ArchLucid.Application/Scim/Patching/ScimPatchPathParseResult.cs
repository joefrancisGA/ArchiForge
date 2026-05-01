namespace ArchLucid.Application.Scim.Patching;

/// <summary>Outcome of interpreting a PATCH <c>path</c> string (RFC 7644 §3.5.2 <c>valuePath</c> subset).</summary>
public abstract record ScimPatchPathParseOutcome;

/// <summary>Unfiltered attribute path (<c>userName</c>, <c>members</c>, etc.).</summary>
public sealed record ScimPatchFlatAttributePathOutcome(string AttributePath) : ScimPatchPathParseOutcome;

/// <summary><c>members[value eq "..."]</c> optionally followed by <c>.subAttr</c> (<c>active</c> only).</summary>
public sealed record ScimPatchMembersFilteredPathOutcome(Guid ReferenceUserId, string? SubAttribute)
    : ScimPatchPathParseOutcome;

/// <summary>Bracketed attribute path ArchLucid does not implement (valid-looking SCIM grammar subset).</summary>
public sealed record ScimPatchPathNotImplementedOutcome(string Detail) : ScimPatchPathParseOutcome;

/// <summary>Path is not valid for group membership PATCH (<c>invalidPath</c>).</summary>
public sealed record ScimPatchPathInvalidOutcome(string Detail) : ScimPatchPathParseOutcome;
