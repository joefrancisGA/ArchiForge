"use client";

import { useCallback, useState } from "react";

import { Button } from "@/components/ui/button";

/**
 * Operator-facing support page (PENDING_QUESTIONS.md item 37, owner decisions F + G,
 * 2026-04-23). One-button download of a freshly-assembled, redacted support bundle ZIP
 * gated by `ExecuteAuthority` server-side. The redaction-policy sub-question (item 37
 * part c) is still open at owner level — surfaced here so operators don't assume the
 * default policy is final.
 */
export default function AdminSupportPage() {
  const [downloading, setDownloading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const onDownload = useCallback(async () => {
    setDownloading(true);
    setError(null);

    try {
      const response = await fetch("/api/proxy/v1/admin/support-bundle", {
        method: "POST",
        headers: { Accept: "application/zip" },
      });

      if (!response.ok) {
        const text = await response.text().catch(() => "");
        throw new Error(
          `Support-bundle download failed (HTTP ${response.status}). ${text.slice(0, 280)}`,
        );
      }

      const disposition = response.headers.get("content-disposition") ?? "";
      const filename =
        parseFilenameFromContentDisposition(disposition) ??
        defaultSupportBundleFilename();

      const blob = await response.blob();
      const url = URL.createObjectURL(blob);

      const a = document.createElement("a");
      a.href = url;
      a.download = filename;
      document.body.appendChild(a);
      a.click();
      document.body.removeChild(a);
      URL.revokeObjectURL(url);
    } catch (cause) {
      setError(cause instanceof Error ? cause.message : String(cause));
    } finally {
      setDownloading(false);
    }
  }, []);

  return (
    <main className="mx-auto max-w-2xl space-y-6">
      <div>
        <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Support</h1>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">
          Download a redacted support bundle to attach to an inbound support ticket. The bundle includes host build
          identity, an environment-variable snapshot (secret-shaped names show <code>(set)</code>/<code>(not set)</code>{" "}
          only), and references to API + documentation correlation tips.
        </p>
      </div>

      <div className="space-y-3 rounded-lg border border-neutral-200 bg-white p-4 shadow-sm dark:border-neutral-800 dark:bg-neutral-950">
        <Button
          type="button"
          data-testid="admin-support-download-bundle"
          disabled={downloading}
          onClick={() => void onDownload()}
        >
          {downloading ? "Preparing bundle…" : "Download support bundle"}
        </Button>

        <p className="text-xs text-neutral-600 dark:text-neutral-400">
          The endpoint requires <strong>elevated permissions</strong> on the API for your tenant.
          Owner approval of the pre-forwarding redaction policy (item 37 part c) is still pending — review the bundle
          contents before forwarding to a third party.
        </p>

        {error !== null ? (
          <p
            role="alert"
            className="rounded-md border border-rose-300 bg-rose-50 p-2 text-sm text-rose-900 dark:border-rose-800 dark:bg-rose-950 dark:text-rose-100"
            data-testid="admin-support-download-error"
          >
            {error}
          </p>
        ) : null}
      </div>
    </main>
  );
}

function defaultSupportBundleFilename(): string {
  const now = new Date();
  const stamp = now.toISOString().replace(/[:T-]/g, "").slice(0, 15) + "Z";
  return `archlucid-support-bundle-${stamp}.zip`;
}

function parseFilenameFromContentDisposition(header: string): string | null {
  if (header.length === 0) return null;

  const utf8Match = /filename\*=UTF-8''([^;]+)/i.exec(header);

  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1].trim());
    } catch {
      // fall through to plain filename match
    }
  }

  const plainMatch = /filename="?([^";]+)"?/i.exec(header);
  return plainMatch?.[1]?.trim() ?? null;
}
