import type { Metadata } from "next";
import Link from "next/link";

import { NewRunWizardClient } from "./NewRunWizardClient";

export const metadata: Metadata = {
  title: "New run",
};

export default function NewRunPage() {
  return (
    <main>
      <h2>New run</h2>
      <p style={{ marginTop: 8 }}>
        <Link href="/runs">Runs list</Link>
        {" · "}
        <Link href="/">Home</Link>
      </p>
      <p
        style={{
          maxWidth: 640,
          marginTop: 12,
          padding: "12px 14px",
          background: "#f8fafc",
          border: "1px solid #e2e8f0",
          borderRadius: 8,
          fontSize: 14,
          color: "#475569",
          lineHeight: 1.55,
        }}
      >
        <strong>What happens next:</strong> this wizard <strong>creates</strong> the run. The pipeline executes
        asynchronously. On the last step, open <strong>run detail</strong> to watch the authority chain. When the
        run is ready, <strong>commit</strong> the manifest via API/CLI (see run detail copy) — then manifest
        summary, artifacts, and ZIP exports appear.
      </p>
      <NewRunWizardClient />
    </main>
  );
}
