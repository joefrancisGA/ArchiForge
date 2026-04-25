/** Default dev scope (matches ArchLucid.Core.Scoping.ScopeIds). */
export const DEV_SCOPE_TENANT_ID = "11111111-1111-1111-1111-111111111111";
export const DEV_SCOPE_WORKSPACE_ID = "22222222-2222-2222-2222-222222222222";
export const DEV_SCOPE_PROJECT_ID = "33333333-3333-3333-3333-333333333333";

export function getScopeHeaders(): Record<string, string> {
  return {
    "x-tenant-id": DEV_SCOPE_TENANT_ID,
    "x-workspace-id": DEV_SCOPE_WORKSPACE_ID,
    "x-project-id": DEV_SCOPE_PROJECT_ID,
  };
}
