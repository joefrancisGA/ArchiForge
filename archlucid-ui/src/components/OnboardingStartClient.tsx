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

type RecoveryCopy = {
  headline: string;
  detail: string;
};

function recoveryCopyForTrialStatusFailure(httpStatus: number): RecoveryCopy {
  if (httpStatus === 401 || httpStatus === 403) {
    return {
      headline: "Sign-in required",
      detail:
        "Finish email verification and sign in with a user that can access this tenant. You can still start an architecture review from Operator home once you are signed in.",
    };
  }

  if (httpStatus === 404) {
    return {
      headline: "Trial workspace not found yet",
      detail:
        "Provisioning may still be running. Retry in a minute, or start a new review request — your workspace may already allow reviews without this panel.",
    };
  }

  if (httpStatus === 429) {
    return {
      headline: "Too many requests",
      detail: "Wait a few seconds and retry. You can still continue with a new review request or open Operator home.",
    };
  }

  if (httpStatus >= 500) {
    return {
      headline: "Service temporarily unavailable",
      detail:
        "The trial-status service is not responding. Retry shortly, or continue — your tenant may still accept review requests.",
    };
  }

  return {
    headline: `Could not load trial workspace (${httpStatus})`,
    detail:
      "Retry, or continue without the seeded sample run. Trial limits may still apply once the service responds.",
  };
}

function recoveryCopyForNetworkError(): RecoveryCopy {
  return {
    headline: "Network error",
    detail:
      "Check your connection, then retry. You can still open the new-review wizard or Operator home to keep going.",
  };
}

/**
 * Post-registration entry: reads `GET /v1/tenant/trial-status` using registration scope headers (pre-OIDC),
 * surfaces limits and the seeded sample run, and links into `/reviews/new` with `sampleRunId` for the wizard.
 */
export function OnboardingStartClient() {
  const [status, setStatus] = useState<TenantTrialStatusResponse | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [errorStatus, setErrorStatus] = useState<number | null>(null);
  const [loading, setLoading] = useState(true);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    setErrorStatus(null);

    try {
      const res = await fetch(
        "/api/proxy/v1/tenant/trial-status",
        mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }),
      );

      if (!res.ok) {
        setErrorStatus(res.status);
        setError(`Trial status request failed (${res.status}).`);
        setStatus(null);

        return;
      }

      const json = (await res.json()) as TenantTrialStatusResponse;
      setStatus(json);
    } catch {
      setError("Could not load trial status.");
      setErrorStatus(null);
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
  const wizardHref = hasSample ? `/reviews/new?sampleRunId=${encodeURIComponent(sampleId)}` : "/reviews/new";

  const recovery: RecoveryCopy | null =
    error !== null && !loading
      ? errorStatus !== null
        ? recoveryCopyForTrialStatusFailure(errorStatus)
        : recoveryCopyForNetworkError()
      : null;

  return (
    <div className="mx-auto max-w-3xl space-y-6">
      {loading ? <p className="text-sm text-neutral-600 dark:text-neutral-400">Loading trial workspace…</p> : null}

      {error !== null && !loading && recovery !== null ? (
        <section
          aria-labelledby="onb-error-heading"
          className="rounded-lg border border-red-200 bg-red-50/90 p-5 text-red-950 dark:border-red-900 dark:bg-red-950/40 dark:text-red-50"
          role="alert"
        >
          <h2 id="onb-error-heading" className="m-0 text-base font-semibold">
            {recovery.headline}
          </h2>
          <p className="m-0 mt-2 text-sm leading-relaxed">{recovery.detail}</p>
          <p className="m-0 mt-2 text-sm font-mono text-red-900/90 dark:text-red-100/90">{error}</p>
          <div className="mt-4 flex flex-wrap gap-2">
            <Button type="button" variant="primary" size="sm" onClick={() => void load()}>
              Retry trial status
            </Button>
            <Button asChild type="button" variant="outline" size="sm">
              <Link href="/reviews/new">Start new review request</Link>
            </Button>
            <Button asChild type="button" variant="outline" size="sm">
              <Link href="/onboarding">Open onboarding checklist</Link>
            </Button>
            <Button asChild type="button" variant="ghost" size="sm">
              <Link href="/">Operator home</Link>
            </Button>
          </div>
        </section>
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
              <Button asChild variant="primary">
                <Link href={wizardHref} data-testid="onboarding-open-wizard-sample">
                  Continue in new-run wizard (sample highlighted)
                </Link>
              </Button>
            ) : null}
            {hasSample ? (
              <Button asChild variant="outline">
                <Link href={`/reviews/${sampleId}`} data-testid="onboarding-open-sample-run">
                  Open example run
                </Link>
              </Button>
            ) : null}
            <Button asChild variant="outline">
              <Link href="/reviews/new">Open new-run wizard</Link>
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
