/**
 * Public OIDC settings (Entra ID / any OIDC provider with PKCE + SPA CORS on the token endpoint).
 * Align scopes with the API app registration so the access token audience matches ArchiForgeAuth:Audience.
 */

export function isJwtAuthMode(): boolean {
  const mode = process.env.NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE ?? "development-bypass";

  return mode === "jwt" || mode === "jwt-bearer";
}

export function getOidcAuthority(): string {
  return process.env.NEXT_PUBLIC_OIDC_AUTHORITY?.trim() ?? "";
}

export function getOidcClientId(): string {
  return process.env.NEXT_PUBLIC_OIDC_CLIENT_ID?.trim() ?? "";
}

export function getOidcScopes(): string {
  const scopes = process.env.NEXT_PUBLIC_OIDC_SCOPES?.trim();

  if (scopes && scopes.length > 0) {
    return scopes;
  }

  return "openid profile offline_access";
}

/**
 * Must match a redirect URI registered on the SPA / public client. Defaults to this origin + /auth/callback.
 */
export function getOidcRedirectUri(): string {
  const fixed = process.env.NEXT_PUBLIC_OIDC_REDIRECT_URI?.trim();

  if (fixed && fixed.length > 0) {
    return fixed;
  }

  if (typeof window !== "undefined") {
    return `${window.location.origin}/auth/callback`;
  }

  return "";
}

export function getOidcPostLogoutRedirectUri(): string {
  const fixed = process.env.NEXT_PUBLIC_OIDC_POST_LOGOUT_REDIRECT_URI?.trim();

  if (fixed && fixed.length > 0) {
    return fixed;
  }

  if (typeof window !== "undefined") {
    return `${window.location.origin}/`;
  }

  return "";
}

export function assertOidcSignInConfig(): { ok: true } | { ok: false; message: string } {
  if (!isJwtAuthMode()) {
    return { ok: false, message: "Set NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE to jwt or jwt-bearer." };
  }

  if (!getOidcAuthority()) {
    return { ok: false, message: "Set NEXT_PUBLIC_OIDC_AUTHORITY (issuer, e.g. https://login.microsoftonline.com/{tenant}/v2.0)." };
  }

  if (!getOidcClientId()) {
    return { ok: false, message: "Set NEXT_PUBLIC_OIDC_CLIENT_ID (SPA / public client id)." };
  }

  if (typeof window !== "undefined" && !getOidcRedirectUri()) {
    return { ok: false, message: "Could not resolve redirect URI." };
  }

  return { ok: true };
}
