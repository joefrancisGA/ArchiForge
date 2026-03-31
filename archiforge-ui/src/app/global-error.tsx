"use client";

import Link from "next/link";
import { useEffect, type CSSProperties } from "react";

import "./globals.css";

const shell: CSSProperties = {
  padding: 24,
  fontFamily: "system-ui, Arial, sans-serif",
};

const callout: CSSProperties = {
  borderRadius: 8,
  padding: "12px 16px",
  marginBottom: 16,
  maxWidth: 720,
  border: "1px solid #b91c1c",
  background: "#fef2f2",
  color: "#7f1d1d",
};

const buttonStyle: CSSProperties = {
  cursor: "pointer",
  padding: "8px 14px",
  fontSize: 14,
  borderRadius: 6,
  border: "1px solid #cbd5e1",
  background: "#fff",
};

/**
 * Replaces the entire root layout when layout.tsx fails. Must define html/body.
 * Keeps styling self-contained so it still renders if layout imports break.
 */
export default function GlobalError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Operator shell global error:", error);
  }, [error]);

  return (
    <html lang="en">
      <body>
        <div style={shell}>
          <h1 style={{ margin: "0 0 12px", fontSize: 26 }}>ArchiForge</h1>
          <div role="alert" style={callout}>
            <strong>The operator shell could not load.</strong>
            <p style={{ margin: "8px 0 0" }}>
              A critical error occurred in the app shell. Try reloading the page or return home.
            </p>
            {process.env.NODE_ENV === "development" ? (
              <pre
                style={{
                  marginTop: 12,
                  fontSize: 12,
                  overflow: "auto",
                  maxHeight: 160,
                  whiteSpace: "pre-wrap",
                }}
              >
                {error.message}
              </pre>
            ) : null}
          </div>
          <div style={{ display: "flex", gap: 12, alignItems: "center", flexWrap: "wrap" }}>
            <button type="button" onClick={() => reset()} style={buttonStyle}>
              Try again
            </button>
            <Link href="/" style={{ fontSize: 14 }}>
              Home
            </Link>
          </div>
        </div>
      </body>
    </html>
  );
}
