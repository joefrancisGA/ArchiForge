"use client";

import { Check, Copy, ExternalLink } from "lucide-react";
import { useCallback, useState } from "react";

import { Button } from "@/components/ui/button";
import { buildTraceViewerUrl } from "@/lib/trace-link";

export interface RunTraceViewerLinkProps {
  /** 32-char hex trace id from the API `X-Trace-Id` header, or null when the header was not returned. */
  traceId: string | null;
}

/**
 * When a trace id is present, shows an optional **View trace** deep link (if `NEXT_PUBLIC_TRACE_VIEWER_URL_TEMPLATE`
 * is set), a monospace preview of the id, and a copy-to-clipboard control.
 */
export function RunTraceViewerLink({ traceId }: RunTraceViewerLinkProps) {
  const [copied, setCopied] = useState(false);
  const traceUrl = buildTraceViewerUrl(traceId ?? undefined);

  const handleCopy = useCallback(async () => {
    if (!traceId) {
      return;
    }

    try {
      await navigator.clipboard.writeText(traceId);
      setCopied(true);
      window.setTimeout(() => setCopied(false), 2000);
    } catch {
      setCopied(false);
    }
  }, [traceId]);

  if (!traceId) {
    return null;
  }

  const preview =
    traceId.length > 8 ? `${traceId.slice(0, 8)}…` : traceId;

  return (
    <div className="mt-1 flex flex-wrap items-center gap-2 text-xs">
      {traceUrl ? (
        <a
          href={traceUrl}
          target="_blank"
          rel="noopener noreferrer"
          className="inline-flex items-center gap-1.5 text-blue-600 hover:underline dark:text-blue-400"
        >
          <ExternalLink className="h-3.5 w-3.5 shrink-0" aria-hidden />
          View trace
        </a>
      ) : null}
      <span
        className="font-mono text-neutral-600 dark:text-neutral-400"
        title={traceId}
      >
        {preview}
      </span>
      <Button
        type="button"
        variant="ghost"
        size="sm"
        className="h-7 px-2 text-neutral-600 dark:text-neutral-400"
        onClick={handleCopy}
        aria-label="Copy full trace ID"
      >
        {copied ? (
          <Check className="h-3.5 w-3.5" aria-hidden />
        ) : (
          <Copy className="h-3.5 w-3.5" aria-hidden />
        )}
      </Button>
    </div>
  );
}
