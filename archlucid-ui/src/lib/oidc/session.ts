import {
  getOidcAuthority,
  getOidcClientId,
  getOidcPostLogoutRedirectUri,
  isJwtAuthMode,
} from "@/lib/oidc/config";
import { loadDiscoveryDocument } from "@/lib/oidc/discovery";
import {
  OIDC_ACCESS_TOKEN_KEY,
  OIDC_CODE_VERIFIER_KEY,
  OIDC_EXPIRES_AT_MS_KEY,
  OIDC_ID_TOKEN_KEY,
  OIDC_NONCE_KEY,
  OIDC_OAUTH_STATE_KEY,
  OIDC_REFRESH_TOKEN_KEY,
} from "@/lib/oidc/storage-keys";
import { decodeJwtPayload, pickDisplayNameFromPayload } from "@/lib/oidc/jwt-payload";
import { refreshAccessToken } from "@/lib/oidc/token-client";
import type { OidcTokenResponse } from "@/lib/oidc/token-client";

const EXPIRY_SKEW_MS = 60_000;

export function persistTokenResponse(tokens: OidcTokenResponse): void {
  sessionStorage.setItem(OIDC_ACCESS_TOKEN_KEY, tokens.access_token);

  if (tokens.refresh_token) {
    sessionStorage.setItem(OIDC_REFRESH_TOKEN_KEY, tokens.refresh_token);
  }

  if (tokens.id_token) {
    sessionStorage.setItem(OIDC_ID_TOKEN_KEY, tokens.id_token);
  }

  const expiresInSec = typeof tokens.expires_in === "number" ? tokens.expires_in : 3600;
  const expiresAtMs = Date.now() + expiresInSec * 1000;

  sessionStorage.setItem(OIDC_EXPIRES_AT_MS_KEY, String(expiresAtMs));
}

export function clearOidcSession(): void {
  sessionStorage.removeItem(OIDC_ACCESS_TOKEN_KEY);
  sessionStorage.removeItem(OIDC_REFRESH_TOKEN_KEY);
  sessionStorage.removeItem(OIDC_EXPIRES_AT_MS_KEY);
  sessionStorage.removeItem(OIDC_ID_TOKEN_KEY);
  sessionStorage.removeItem(OIDC_OAUTH_STATE_KEY);
  sessionStorage.removeItem(OIDC_CODE_VERIFIER_KEY);
}

export function storePkceState(state: string, codeVerifier: string, nonce: string): void {
  sessionStorage.setItem(OIDC_OAUTH_STATE_KEY, state);
  sessionStorage.setItem(OIDC_CODE_VERIFIER_KEY, codeVerifier);
  sessionStorage.setItem(OIDC_NONCE_KEY, nonce);
}

export function readPkceState(): { state: string; codeVerifier: string; nonce: string } | null {
  const state = sessionStorage.getItem(OIDC_OAUTH_STATE_KEY);
  const codeVerifier = sessionStorage.getItem(OIDC_CODE_VERIFIER_KEY);
  const nonce = sessionStorage.getItem(OIDC_NONCE_KEY);

  if (!state || !codeVerifier || !nonce) {
    return null;
  }

  return { state, codeVerifier, nonce };
}

export function consumePkceState(): { state: string; codeVerifier: string; nonce: string } | null {
  const pair = readPkceState();

  if (!pair) {
    return null;
  }

  sessionStorage.removeItem(OIDC_OAUTH_STATE_KEY);
  sessionStorage.removeItem(OIDC_CODE_VERIFIER_KEY);
  sessionStorage.removeItem(OIDC_NONCE_KEY);

  return pair;
}

function getExpiresAtMs(): number {
  const raw = sessionStorage.getItem(OIDC_EXPIRES_AT_MS_KEY);

  return Number(raw ?? "0");
}

/**
 * Access token for Authorization: Bearer (undefined if missing or past skewed expiry).
 */
export function getAccessTokenForApi(): string | undefined {
  if (typeof sessionStorage === "undefined") {
    return undefined;
  }

  const exp = getExpiresAtMs();

  if (Date.now() >= exp - EXPIRY_SKEW_MS) {
    return undefined;
  }

  const token = sessionStorage.getItem(OIDC_ACCESS_TOKEN_KEY);

  return token && token.length > 0 ? token : undefined;
}

/**
 * Refreshes using refresh_token when within skew of expiry. No-op when not in browser JWT mode.
 */
export async function ensureAccessTokenFresh(): Promise<void> {
  if (typeof window === "undefined" || !isJwtAuthMode()) {
    return;
  }

  const exp = getExpiresAtMs();
  const refresh = sessionStorage.getItem(OIDC_REFRESH_TOKEN_KEY);
  const authority = getOidcAuthority();
  const clientId = getOidcClientId();

  if (!refresh || !authority || !clientId) {
    return;
  }

  if (Date.now() < exp - EXPIRY_SKEW_MS) {
    return;
  }

  try {
    const doc = await loadDiscoveryDocument(authority);
    const tokens = await refreshAccessToken({
      tokenEndpoint: doc.token_endpoint,
      clientId,
      refreshToken: refresh,
    });

    persistTokenResponse(tokens);
  } catch {
    clearOidcSession();
  }
}

export function readSignedInDisplayName(): string | null {
  if (typeof sessionStorage === "undefined") {
    return null;
  }

  const access = sessionStorage.getItem(OIDC_ACCESS_TOKEN_KEY);
  const idTok = sessionStorage.getItem(OIDC_ID_TOKEN_KEY);

  if (access) {
    const fromAccess = pickDisplayNameFromPayload(decodeJwtPayload(access));

    if (fromAccess) {
      return fromAccess;
    }
  }

  if (idTok) {
    return pickDisplayNameFromPayload(decodeJwtPayload(idTok));
  }

  return null;
}

export function isLikelySignedIn(): boolean {
  if (typeof sessionStorage === "undefined") {
    return false;
  }

  const token = sessionStorage.getItem(OIDC_ACCESS_TOKEN_KEY);

  return Boolean(token && token.length > 0 && Date.now() < getExpiresAtMs() - EXPIRY_SKEW_MS);
}

/**
 * Clears local session and redirects to the IdP end_session endpoint when available (OIDC RP-initiated logout).
 */
export async function signOutAndRedirectHome(): Promise<void> {
  if (typeof window === "undefined") {
    return;
  }

  const idToken = sessionStorage.getItem(OIDC_ID_TOKEN_KEY);
  const authority = getOidcAuthority();

  clearOidcSession();

  if (!authority) {
    window.location.assign("/");

    return;
  }

  try {
    const doc = await loadDiscoveryDocument(authority);

    if (doc.end_session_endpoint && idToken && idToken.length > 0) {
      const url = new URL(doc.end_session_endpoint);

      url.searchParams.set("id_token_hint", idToken);
      url.searchParams.set("post_logout_redirect_uri", getOidcPostLogoutRedirectUri());
      window.location.assign(url.toString());

      return;
    }
  } catch {
    /* ignore discovery errors */
  }

  window.location.assign("/");
}
