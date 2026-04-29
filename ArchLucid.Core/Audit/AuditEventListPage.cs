namespace ArchLucid.Core.Audit;

/// <summary>Scoped audit listing using keyset predicates (no OFFSET); <see cref="HasMore"/> supports take+1 semantics.</summary>
public sealed record AuditEventListPage(IReadOnlyList<AuditEvent> Items, bool HasMore);
