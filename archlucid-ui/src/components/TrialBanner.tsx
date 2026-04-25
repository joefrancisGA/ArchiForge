"use client";

import { X } from "lucide-react";
import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { AUTH_MODE } from "@/lib/auth-config";
import { isJwtAuthMode } from "@/lib/oidc/config";
import { isLikelySignedIn } from "@/lib/oidc/session";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { isTrialBannerSnoozed, snoozeTrialBanner24h } from "@/lib/trial-banner-dismiss";
import { showError, showSuccess } from "@/lib/toast";
import { Button } from "@/components/ui/button";

type TrialStatusPayload = {
  status?: string;
  daysRemaining?: number | null;
};

function shouldShowTrialStrip(status: string | undefined): boolean {
  if (!status || status === "None" || status === "Converted") {
    return false;
  }

  return status === "Active" || status === "Expired" || status === "ReadOnly";
}

/** Sticky trial callout in the operator shell; dismiss hides for 24h. */
export function TrialBanner() {
  const [visible, setVisible] = useState(false);
  const [payload, setPayload] = useState<TrialStatusPayload | null>(null);
  const [hydrated, setHydrated] = useState(false);

  const refresh = useCallback(async () => {
    if (AUTH_MODE !== "development-bypass" && isJwtAuthMode() && !isLikelySignedIn()) {
      setVisible(false);

      return;
    }

    if (isTrialBannerSnoozed()) {
      setVisible(false);

      return;
    }

    try {
      const res = await fetch(
        "/api/proxy/v1/tenant/trial-status",
        mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
      );

      if (!res.ok) {
        setVisible(false);

        return;
      }

      const json = (await res.json()) as TrialStatusPayload;
      setPayload(json);

      setVisible(shouldShowTrialStrip(json.status));
    } catch {
      setVisible(false);
    }
  }, []);

  useEffect(() => {
    setHydrated(true);
    void refresh();
  }, [refresh]);

  const onConvert = async () => {
    try {
      const res = await fetch(
        "/api/proxy/v1/tenant/billing/checkout",
        mergeRegistrationScopeForProxy({ method: "POST", headers: { Accept: "application/json" } }),
      );

      const json = (await res.json().catch(() => null)) as { status?: string } | null;

      if (!res.ok) {
        showError("Billing", `Checkout request failed (${res.status}).`);

        return;
      }

      if (json?.status === "not_configured") {
        showSuccess("Billing: checkout will open here once billing is connected for your tenant.");

        return;
      }

      showSuccess("Billing: request accepted.");
    } catch (e: unknown) {
      const message = e instanceof Error ? e.message : "Request failed.";
      showError("Billing", message);
    }
  };

  if (!hydrated || !visible || !payload) {
    return null;
  }

  const days = payload.daysRemaining;
  const daysLabel =
    typeof days === "number" ? `${days} day${days === 1 ? "" : "s"} remaining on trial` : "Trial status updated";

  return (
    <div
      role="region"
      aria-label="Trial subscription"
      className="mb-4 flex flex-wrap items-start justify-between gap-3 rounded-lg border border-amber-300 bg-amber-50 p-3 text-sm text-amber-950 dark:border-amber-700 dark:bg-amber-950/40 dark:text-amber-50"
    >
      <div>
        <strong className="font-semibold">Trial workspace</strong>
        <span className="mx-2 text-amber-800 dark:text-amber-200">·</span>
        <span>{daysLabel}</span>
        <div className="mt-2 flex flex-wrap gap-2">
          <Button type="button" size="sm" className="bg-amber-800 text-white hover:bg-amber-900" onClick={onConvert}>
            Convert to paid
          </Button>
          <Button asChild type="button" size="sm" variant="outline">
            <Link href="/getting-started?source=registration">Trial checklist</Link>
          </Button>
        </div>
      </div>
      <Button
        type="button"
        variant="ghost"
        size="icon"
        className="h-8 w-8 shrink-0 text-amber-900 hover:bg-amber-100 dark:text-amber-100 dark:hover:bg-amber-900/60"
        aria-label="Dismiss trial banner for 24 hours"
        onClick={() => {
          snoozeTrialBanner24h();
          setVisible(false);
        }}
      >
        <X className="h-4 w-4" aria-hidden />
      </Button>
    </div>
  );
}
