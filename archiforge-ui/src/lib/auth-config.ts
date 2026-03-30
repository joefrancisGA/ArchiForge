/**
 * Authentication mode for the operator shell.
 * Values: "development-bypass" (default, no sign-in), "jwt" / "jwt-bearer" (OIDC tokens).
 * Must match the API's ArchiForgeAuth:Mode configuration.
 */
export const AUTH_MODE =
  process.env.NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE ?? "development-bypass";
