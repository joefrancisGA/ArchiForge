"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";
import { readLastRegistrationPayload } from "@/lib/registration-session";

type TenantTrialStatusResponse = {
  status?: string;
  daysRemaining?: number | null;
  trialStartUtc?: string | null;
  trialExpiresUtc?: string | null;
  trialRunsUsed?: number;
  trialRunsLimit?: number | null;
  trialSeatsUsed?: number;
  trialSeatsLimit?: number | null;
  trialSampleRunId?: string | null;
};

/**
 * Post-registration entry: reads `GET /v1/tenant/trial-status` using registration scope headers (pre-OIDC),
 * surfaces limits and the seeded sample run, and links into `/runs/new` with `sampleRunId` for the wizard.
 */
export function OnboardingStartClient() {
  const [status, setStatus] = useState<TenantTrialStatusResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);

    try {
      const res = await fetch(
        "/api/proxy/v1/tenant/trial-status",
        mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
      );

      if (!res.ok) {
        setError(`Trial status request failed (${res.status}).`);
        setStatus(null);

        return;
      }

      const json = (await res.json()) as TenantTrialStatusResponse;
      setStatus(json);
    } catch {
      setError("Could not load trial status.");
      setStatus(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const reg = readLastRegistrationPayload();
  const sampleId = status?.trialSampleRunId?.trim() ?? "";
  const hasSample = sampleId.length > 0;
  const wizardHref = hasSample ? `/runs/new?sampleRunId=${encodeURIComponent(sampleId)}` : "/runs/new";

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {loading ? <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading trial workspace…</p> : null}

      {error !== null && !loading ? (
        <p className="text-sm text-red-600" role="alert">
          {error}{" "}
          <Button type="button" variant="link" className="h-auto p-0 align-baseline" onClick={() => void load()}>
            Retry
          </Button>
        </p>
      ) : null}

      {!loading && status !== null ? (
        <section aria-labelledby="onb-status-heading" className="rounded-lg border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-900">
          <h2 id="onb-status-heading" className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">
            Trial workspace
          </h2>
          <dl className="mt-3 grid gap-2 text-sm text-neutral-800 dark:text-neutral-200 sm:grid-cols-2">
            <div>
              <dt className="text-neutral-500 dark:text-neutral-400">Status</dt>
              <dd className="font-medium">{status.status ?? "Unknown"}</dd>
            </div>
            <div>
              <dt className="text-neutral-500 dark:text-neutral-400">Days remaining</dt>
              <dd className="font-medium">
                {typeof status.daysRemaining === "number" ? status.daysRemaining : "—"}
              </dd>
            </div>
            <div>
              <dt className="text-neutral-500 dark:text-neutral-400">Runs used</dt>
              <dd className="font-medium">
                {status.trialRunsUsed ?? 0}
                {typeof status.trialRunsLimit === "number" ? ` / ${status.trialRunsLimit}` : ""}
              </dd>
            </div>
            <div>
              <dt className="text-neutral-500 dark:text-neutral-400">Seats used</dt>
              <dd className="font-medium">
                {status.trialSeatsUsed ?? 0}
                {typeof status.trialSeatsLimit === "number" ? ` / ${status.trialSeatsLimit}` : ""}
              </dd>
            </div>
          </dl>

          {reg?.organizationName !== undefined && reg.organizationName.length > 0 ? (
            <p className="mt-3 text-sm text-neutral-600 dark:text-neutral-400">
              Organization: <strong>{reg.organizationName}</strong>
            </p>
          ) : null}

          <div className="mt-5 flex flex-wrap gap-3">
            {hasSample ? (
              <Button asChild className="bg-teal-700 text-white hover:bg-teal-800">
                <Link href={wizardHref} data-testid="onboarding-open-wizard-sample">
                  Continue in new-run wizard (sample highlighted)
                </Link>
              </Button>
            ) : null}
            {hasSample ? (
              <Button asChild variant="outline">
                <Link href={`/runs/${sampleId}`} data-testid="onboarding-open-sample-run">
                  Open seeded sample run
                </Link>
              </Button>
            ) : null}
            <Button asChild variant="outline">
              <Link href="/runs/new">Open new-run wizard</Link>
            </Button>
            <Button asChild variant="ghost">
              <Link href="/">Operator home</Link>
            </Button>
          </div>
        </section>
      ) : null}
    </div>
  );
}
