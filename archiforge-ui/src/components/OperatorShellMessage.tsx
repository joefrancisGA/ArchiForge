import type { CSSProperties, ReactNode } from "react";

const calloutBase: CSSProperties = {
  borderRadius: 8,
  padding: "12px 16px",
  marginBottom: 16,
  maxWidth: 720,
};

/**
 * API / configuration failures on review pages (server-rendered).
 */
export function OperatorErrorCallout({ children }: { children: ReactNode }) {
  return (
    <div
      role="alert"
      style={{
        ...calloutBase,
        border: "1px solid #b91c1c",
        background: "#fef2f2",
        color: "#7f1d1d",
      }}
    >
      {children}
    </div>
  );
}

/**
 * Empty collections or blocked review steps (e.g. run not committed).
 */
export function OperatorEmptyState({
  title,
  children,
}: {
  title: string;
  children: ReactNode;
}) {
  return (
    <div
      role="status"
      style={{
        ...calloutBase,
        border: "1px solid #d4d4d4",
        background: "#fafafa",
        color: "#404040",
      }}
    >
      <strong>{title}</strong>
      <div style={{ marginTop: 8 }}>{children}</div>
    </div>
  );
}

/**
 * Non-fatal secondary fetch issues (manifest summary, artifact list).
 */
export function OperatorWarningCallout({ children }: { children: ReactNode }) {
  return (
    <div
      role="status"
      style={{
        ...calloutBase,
        border: "1px solid #ca8a04",
        background: "#fffbeb",
        color: "#713f12",
      }}
    >
      {children}
    </div>
  );
}
