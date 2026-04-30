"use client";

import { useEffect, useState } from "react";
import Link from "next/link";

import { Button } from "@/components/ui/button";
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
const REDIRECT_FALLBACK_MS = 8000;

export function SignInClient() {
  const [error, setError] = useState<string | null>(null);
  const [showSlowHint, setShowSlowHint] = useState(false);

  useEffect(() => {
    const t = window.setTimeout(() => {
      setShowSlowHint(true);
    }, REDIRECT_FALLBACK_MS);

    return () => {
      window.clearTimeout(t);
    };
  }, []);

  useEffect(() => {
    if (!isJwtAuthMode()) {
      setError("JWT / OIDC mode is not enabled (set NEXT_PUBLIC_ARCHLUCID_AUTH_MODE=jwt).");

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
        const nonce = randomOpaqueState();

        storePkceState(state, verifier, nonce);
        const doc = await loadDiscoveryDocument(authority);
        const url = buildAuthorizeUrl({
          doc,
          clientId,
          redirectUri,
          scope,
          state,
          codeChallenge: challenge,
          nonce,
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
        <div className="mt-4 flex flex-wrap items-center gap-3">
          <Button asChild variant="default" size="sm">
            <Link href="/auth/signin">Try signing in again</Link>
          </Button>
          <Button asChild variant="outline" size="sm">
            <Link href="/help">Help</Link>
          </Button>
          <Link href="/" className="text-sm text-teal-800 underline dark:text-teal-300">
            Back to home
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 560 }}>
      <h2 style={{ marginTop: 0 }}>Sign-in</h2>
      <p>Redirecting to your identity provider…</p>
      {showSlowHint ? (
        <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
          Taking longer than expected?{" "}
          <Link className="text-teal-700 underline dark:text-teal-300" href="/auth/signin">
            Try again
          </Link>{" "}
          or <Link className="text-teal-700 underline dark:text-teal-300" href="/">
            return home
          </Link>
          .
        </p>
      ) : null}
    </div>
  );
}
