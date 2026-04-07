/** Default dev scope (matches ArchiForge.Core.Scoping.ScopeIds). */
export function getScopeHeaders(): Record<string, string> {
  return {
    "x-tenant-id": "11111111-1111-1111-1111-111111111111",
    "x-workspace-id": "22222222-2222-2222-2222-222222222222",
    "x-project-id": "33333333-3333-3333-3333-333333333333",
  };
}
