import type { CSSProperties } from "react";

import { OperatorWarningCallout } from "@/components/OperatorShellMessage";
import type { PreparedArtifactBody } from "@/lib/artifact-review-helpers";

const preBox: CSSProperties = {
  margin: 0,
  padding: 16,
  border: "1px solid #e2e8f0",
  borderRadius: 8,
  background: "#fff",
  whiteSpace: "pre-wrap",
  wordBreak: "break-word",
  fontSize: 14,
  lineHeight: 1.55,
  fontFamily: 'ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, "Liberation Mono", monospace',
  maxHeight: "min(70vh, 720px)",
  overflow: "auto",
};

/**
 * Human-readable panel plus optional raw disclosure (deterministic; no HTML injection).
 */
export function ArtifactReviewContent(props: {
  prepared: PreparedArtifactBody;
  contentType: string;
  byteLength: number;
  truncated: boolean;
  contentError: string | null;
}) {
  const { prepared, contentType, byteLength, truncated, contentError } = props;

  if (contentError) {
    return (
      <OperatorWarningCallout>
        <strong>In-shell preview unavailable.</strong>
        <p style={{ margin: "8px 0 0" }}>{contentError}</p>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>
          Use <strong>Download</strong> to open the artifact locally. Descriptor metadata above is still
          valid when the download endpoint succeeds.
        </p>
      </OperatorWarningCallout>
    );
  }

  const rawIsDistinct = prepared.readableText !== prepared.rawText;

  const caption =
    prepared.viewKind === "markdown"
      ? "Markdown (rendered as pre-wrapped text; download for editors or viewers)"
      : prepared.viewKind === "mermaid"
        ? "Mermaid source (paste into a Mermaid viewer or download this file)"
        : prepared.viewKind === "json"
          ? prepared.jsonPrettyFailed
            ? "JSON (invalid — showing raw bytes as text)"
            : "JSON (pretty-printed for review)"
          : "Text content";

  return (
    <div>
      {truncated && (
        <OperatorWarningCallout>
          <strong>Preview truncated.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>
            Showing the first portion of this artifact ({byteLength.toLocaleString()} bytes total). Download
            for the full file.
          </p>
        </OperatorWarningCallout>
      )}

      <p style={{ margin: "0 0 8px", fontSize: 13, color: "#64748b" }}>
        {caption} · <code>{contentType}</code> · {byteLength.toLocaleString()} bytes
      </p>

      <pre style={preBox}>{prepared.readableText}</pre>

      <details style={{ marginTop: 16 }}>
        <summary style={{ cursor: "pointer", fontWeight: 600, color: "#334155" }}>
          Raw UTF-8 content
          {rawIsDistinct ? " (exact, unmodified from API)" : " (same as readable above)"}
        </summary>
        <pre style={{ ...preBox, marginTop: 12, background: "#f8fafc" }}>{prepared.rawText}</pre>
      </details>
    </div>
  );
}
