"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { fetchOperatorNextBestActions, type OperatorNextBestActionDto } from "@/lib/api";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { isStaticDemoPayloadFallbackEnabled } from "@/lib/operator-static-demo";
import { cn } from "@/lib/utils";

/**
 * Tenant-scoped next-best-action rail for operator home. Data comes from SQL-backed signals via
 * GET /v1/tenant/customer-success/next-actions.
 */
export function OperatorNextActionsCard() {
  const demoUi = isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled();
  const [items, setItems] = useState<OperatorNextBestActionDto[] | null>(null);
  const [phase, setPhase] = useState<"idle" | "loading" | "ready" | "error">("idle");

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      setPhase("loading");

      try {
        const data = await fetchOperatorNextBestActions();

        if (!cancelled) {
          setItems(data);
          setPhase("ready");
        }
      } catch {
        if (!cancelled) {
          setPhase("error");
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  if (phase === "error") {
    return (
      <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400" role="status">
        Next steps unavailable (check tenant tier or sign-in).
      </p>
    );
  }

  if (phase === "loading") {
    if (demoUi) {
      return null;
    }

    return <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">Loading recommended next steps…</p>;
  }

  if (items === null || items.length === 0) {
    return null;
  }

  return (
    <section
      aria-labelledby="operator-next-actions-heading"
      className="rounded-lg border border-teal-200/80 bg-teal-50/40 p-3 dark:border-teal-900 dark:bg-teal-950/25"
    >
      <h3
        id="operator-next-actions-heading"
        className="m-0 text-sm font-semibold text-neutral-900 dark:text-neutral-100"
      >
        Recommended next steps
      </h3>
      <ul className="mt-2 space-y-2">
        {items.map((item) => (
          <li key={item.actionId} className="text-xs text-neutral-800 dark:text-neutral-200">
            <Link
              href={item.href}
              className={cn("font-medium text-teal-800 underline underline-offset-2 dark:text-teal-300")}
            >
              {item.title}
            </Link>
            <p className="m-0 mt-0.5 text-[11px] text-neutral-600 dark:text-neutral-400">{item.reason}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}
