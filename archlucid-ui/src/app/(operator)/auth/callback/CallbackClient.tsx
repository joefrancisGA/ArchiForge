"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";

import {
  assertOidcSignInConfig,
  getOidcAuthority,
  getOidcClientId,
  getOidcRedirectUri,
  isJwtAuthMode,
} from "@/lib/oidc/config";
import { loadDiscoveryDocument } from "@/lib/oidc/discovery";
import { exchangeAuthorizationCode } from "@/lib/oidc/token-client";
import { decodeJwtPayload, readNonceFromPayload } from "@/lib/oidc/jwt-payload";
import { consumePkceState, persistTokenResponse } from "@/lib/oidc/session";
import { clearLastRegistrationPayload } from "@/lib/registration-session";

/**
 * OAuth2 authorization-code callback: exchanges ?code= for tokens (PKCE, public client).
 */
export function CallbackClient() {
  const searchParams = useSearchParams();
  const [message, setMessage] = useState<string>("Completing sign-in…");
  const [failed, setFailed] = useState(false);

  const oauthError = searchParams.get("error");
  const oauthErrorDescription = searchParams.get("error_description");
  const code = searchParams.get("code");
  const state = searchParams.get("state");

  useEffect(() => {
    if (!isJwtAuthMode()) {
      setFailed(true);
      setMessage("JWT / OIDC mode is not enabled.");

      return;
    }

    const cfg = assertOidcSignInConfig();

    if (!cfg.ok) {
      setFailed(true);
      setMessage(cfg.message);

      return;
    }

    if (oauthError) {
      setFailed(true);
      setMessage(
        [oauthError, oauthErrorDescription].filter(Boolean).join(": ") || "Authorization failed.",
      );

      return;
    }

    if (!code || !state) {
      setFailed(true);
      setMessage("Missing authorization code or state.");

      return;
    }

    const stored = consumePkceState();

    if (!stored || stored.state !== state) {
      setFailed(true);
      setMessage("Invalid or expired sign-in state. Try signing in again.");

      return;
    }

    let cancelled = false;

    void (async () => {
      try {
        const authority = getOidcAuthority();
        const clientId = getOidcClientId();
        const redirectUri = getOidcRedirectUri();
        const doc = await loadDiscoveryDocument(authority);
        const tokens = await exchangeAuthorizationCode({
          tokenEndpoint: doc.token_endpoint,
          clientId,
          code,
          redirectUri,
          codeVerifier: stored.codeVerifier,
        });

        if (cancelled) {
          return;
        }

        if (tokens.id_token) {
          const idNonce = readNonceFromPayload(decodeJwtPayload(tokens.id_token));

          if (idNonce !== stored.nonce) {
            setFailed(true);
            setMessage("Sign-in failed: ID token nonce did not match (possible replay). Try again.");

            return;
          }
        }

        clearLastRegistrationPayload();
        persistTokenResponse(tokens);
        window.location.replace("/");
      } catch (e) {
        if (!cancelled) {
          setFailed(true);
          setMessage(e instanceof Error ? e.message : String(e));
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, [code, oauthError, oauthErrorDescription, state]);

  return (
    <div style={{ maxWidth: 560 }}>
      <h2 style={{ marginTop: 0 }}>{failed ? "Sign-in was not completed" : "Completing sign-in"}</h2>
      <p style={{ color: failed ? "#b91c1c" : undefined }}>{message}</p>
      {failed ? (
        <div className="mt-4 space-y-3">
          <Button asChild variant="default" size="sm" className="w-fit">
            <Link href="/auth/signin">Try signing in again</Link>
          </Button>
          <p className="text-sm text-neutral-700 dark:text-neutral-300">
            Need context for this screen?{" "}
            <Link className="font-medium text-teal-700 underline dark:text-teal-300" href="/help">
              Open Help
            </Link>{" "}
            or{" "}
            <Link className="font-medium text-teal-700 underline dark:text-teal-300" href="/">
              return home
            </Link>
            .
          </p>
        </div>
      ) : null}
    </div>
  );
}
