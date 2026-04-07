"use client";

import Link from "next/link";
import { useEffect, type CSSProperties } from "react";

import { OperatorErrorCallout } from "@/components/OperatorShellMessage";

const buttonStyle: CSSProperties = {
  cursor: "pointer",
  padding: "8px 14px",
  fontSize: 14,
  borderRadius: 6,
  border: "1px solid #cbd5e1",
  background: "#fff",
};

/**
 * Catches errors in route segments below the root layout (pages, nested layouts).
 * Does not catch errors in root layout.tsx — see global-error.tsx.
 */
export default function AppError({
  error,
  reset,
}: {
  error: Error & { digest?: string };
  reset: () => void;
}) {
  useEffect(() => {
    console.error("Operator shell route error:", error);
  }, [error]);

  return (
    <main>
      <OperatorErrorCallout>
        <strong>Something went wrong.</strong>
        <p style={{ margin: "8px 0 0" }}>
          This page hit an unexpected error. Try again, or return to the home page.
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
      </OperatorErrorCallout>
      <div style={{ display: "flex", gap: 12, alignItems: "center", marginTop: 16, flexWrap: "wrap" }}>
        <button type="button" onClick={() => reset()} style={buttonStyle}>
          Try again
        </button>
        <Link href="/" style={{ fontSize: 14 }}>
          Home
        </Link>
      </div>
    </main>
  );
}
