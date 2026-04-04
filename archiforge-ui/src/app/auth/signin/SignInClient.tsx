"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import {
  assertOidcSignInConfig,
  getOidcAuthority,
  getOidcClientId,
  getOidcRedirectUri,
  getOidcScopes,
  isJwtAuthMode,
} from "@/lib/oidc/config";
import { buildAuthorizeUrl } from "@/lib/oidc/build-authorize-url";
import { loadDiscoveryDocument } from "@/lib/oidc/discovery";
import { createPkcePair, randomOpaqueState } from "@/lib/oidc/pkce";
import { isLikelySignedIn, storePkceState } from "@/lib/oidc/session";

/**
 * Starts OIDC authorization code + PKCE against NEXT_PUBLIC_OIDC_* (Entra or any OIDC provider).
 */
export function SignInClient() {
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    if (!isJwtAuthMode()) {
      setError("JWT / OIDC mode is not enabled (set NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE=jwt).");

      return;
    }

    const cfg = assertOidcSignInConfig();

    if (!cfg.ok) {
      setError(cfg.message);

      return;
    }

    if (isLikelySignedIn()) {
      window.location.replace("/");

      return;
    }

    let cancelled = false;

    void (async () => {
      try {
        const authority = getOidcAuthority();
        const clientId = getOidcClientId();
        const redirectUri = getOidcRedirectUri();
        const scope = getOidcScopes();
        const { verifier, challenge } = await createPkcePair();
        const state = randomOpaqueState();

        storePkceState(state, verifier);
        const doc = await loadDiscoveryDocument(authority);
        const url = buildAuthorizeUrl({
          doc,
          clientId,
          redirectUri,
          scope,
          state,
          codeChallenge: challenge,
        });

        if (!cancelled) {
          window.location.assign(url);
        }
      } catch (e) {
        if (!cancelled) {
          setError(e instanceof Error ? e.message : String(e));
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  if (error) {
    return (
      <div style={{ maxWidth: 560 }}>
        <h2 style={{ marginTop: 0 }}>Sign-in</h2>
        <p style={{ color: "#b91c1c" }}>{error}</p>
        <p>
          <Link href="/">Back to home</Link>
        </p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 560 }}>
      <h2 style={{ marginTop: 0 }}>Sign-in</h2>
      <p>Redirecting to your identity provider…</p>
    </div>
  );
}
