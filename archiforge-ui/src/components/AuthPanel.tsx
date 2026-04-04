"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import {
  isLikelySignedIn,
  readSignedInDisplayName,
  signOutAndRedirectHome,
} from "@/lib/oidc/session";

/** Shell header strip: dev bypass notice, or OIDC sign-in / sign-out + display name. */
export function AuthPanel() {
  const [displayName, setDisplayName] = useState<string | null>(null);
  const [signedIn, setSignedIn] = useState(false);

  const refresh = useCallback(() => {
    if (!isJwtAuthMode()) {
      return;
    }

    setSignedIn(isLikelySignedIn());
    setDisplayName(readSignedInDisplayName());
  }, []);

  useEffect(() => {
    refresh();
  }, [refresh]);

  useEffect(() => {
    const onFocus = (): void => {
      refresh();
    };

    window.addEventListener("focus", onFocus);

    return () => window.removeEventListener("focus", onFocus);
  }, [refresh]);

  if (AUTH_MODE === "development-bypass" || !isJwtAuthMode()) {
    return (
      <div
        style={{
          padding: 12,
          border: "1px solid #ddd",
          borderRadius: 8,
          marginBottom: 16,
          background: "#fff",
        }}
      >
        <strong>Auth mode:</strong> Development bypass (API auto-authenticates; no UI sign-in). Set{" "}
        <code style={{ fontSize: 13 }}>NEXT_PUBLIC_ARCHIFORGE_AUTH_MODE=jwt</code> and OIDC env vars
        for Entra / OIDC.
      </div>
    );
  }

  return (
    <div
      style={{
        padding: 12,
        border: "1px solid #ddd",
        borderRadius: 8,
        marginBottom: 16,
        background: "#fff",
        display: "flex",
        flexWrap: "wrap",
        alignItems: "center",
        gap: 12,
        justifyContent: "space-between",
      }}
    >
      <div>
        <strong>Auth:</strong> OIDC (JWT bearer to API via <code>/api/proxy</code>)
        {signedIn && displayName ? (
          <>
            {" "}
            — signed in as <strong>{displayName}</strong>
          </>
        ) : signedIn ? (
          <> — signed in</>
        ) : (
          <> — not signed in</>
        )}
      </div>
      <div style={{ display: "flex", gap: 10, alignItems: "center" }}>
        {!signedIn ? (
          <Link
            href="/auth/signin"
            style={{
              padding: "6px 12px",
              background: "#0f172a",
              color: "#fff",
              borderRadius: 6,
              textDecoration: "none",
              fontSize: 14,
            }}
          >
            Sign in
          </Link>
        ) : (
          <button
            type="button"
            onClick={() => void signOutAndRedirectHome()}
            style={{
              padding: "6px 12px",
              background: "#fff",
              border: "1px solid #cbd5e1",
              borderRadius: 6,
              cursor: "pointer",
              fontSize: 14,
            }}
          >
            Sign out
          </button>
        )}
      </div>
    </div>
  );
}
