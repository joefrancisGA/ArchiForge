"use client";

import { useCallback, useEffect, useState } from "react";

import { ContextualHelp } from "@/components/ContextualHelp";
import { Button } from "@/components/ui/button";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import {
  findCircuitBreakersEntry,
  type HealthReadyResponse,
  type HealthDetailedResponse,
  type VersionInfoResponse,
  type OperatorTaskSuccessRatesResponse,
  parseCircuitGatesFromHealthEntry,
  type CircuitGateRow,
} from "@/lib/health-dashboard-types";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

function statusBadgeClass(status: string): string {
  const s = status.toLowerCase();
  if (s === "healthy" || s === "closed") {
    return "border-emerald-300 bg-emerald-100 text-emerald-950 dark:border-emerald-800 dark:bg-emerald-950/60 dark:text-emerald-100";
  }
  if (s === "degraded" || s === "halfopen") {
    return "border-amber-300 bg-amber-100 text-amber-950 dark:border-amber-800 dark:bg-amber-950/60 dark:text-amber-100";
  }
  if (s === "unhealthy" || s === "open") {
    return "border-rose-300 bg-rose-100 text-rose-950 dark:border-rose-800 dark:bg-rose-950/60 dark:text-rose-100";
  }
  return "border-neutral-300 bg-neutral-100 text-neutral-900 dark:border-neutral-600 dark:bg-neutral-800 dark:text-neutral-100";
}

function largeOverallClass(status: string): string {
  return `inline-flex items-center rounded-lg border px-4 py-2 text-lg font-semibold ${statusBadgeClass(status)}`;
}

/**
 * In-app diagnostics: readiness (`/health/ready`), authenticated circuit data (`/health`), build identity (`/version`),
 * and onboarding funnel counters (`/v1/diagnostics/operator-task-success-rates`).
 */
export default function AdminHealthPage() {
  const [loading, setLoading] = useState(true);
  const [ready, setReady] = useState<HealthReadyResponse | null>(null);
  const [readyError, setReadyError] = useState<string | null>(null);
  const [version, setVersion] = useState<VersionInfoResponse | null>(null);
  const [circuitNote, setCircuitNote] = useState<string | null>(null);
  const [circuitGates, setCircuitGates] = useState<CircuitGateRow[]>([]);
  const [rates, setRates] = useState<OperatorTaskSuccessRatesResponse | null>(null);
  const [ratesNote, setRatesNote] = useState<string | null>(null);

  const refresh = useCallback(async () => {
    setLoading(true);
    setReadyError(null);
    setCircuitNote(null);
    setRatesNote(null);
    const jsonInit = mergeRegistrationScopeForProxy({
      headers: { Accept: "application/json" },
      cache: "no-store",
    });

    try {
      const [readyRes, versionRes, healthRes, ratesRes] = await Promise.all([
        fetch("/api/proxy/health/ready", jsonInit),
        fetch("/api/proxy/version", jsonInit),
        fetch("/api/proxy/health", jsonInit),
        fetch("/api/proxy/v1/diagnostics/operator-task-success-rates", jsonInit),
      ]);

      if (readyRes.ok) {
        const r = (await readyRes.json()) as HealthReadyResponse;
        setReady(r);
      } else {
        setReady(null);
        setReadyError(`Readiness check failed (HTTP ${readyRes.status}).`);
      }

      if (versionRes.ok) {
        setVersion((await versionRes.json()) as VersionInfoResponse);
      } else {
        setVersion(null);
      }

      if (healthRes.status === 401 || healthRes.status === 403) {
        setCircuitGates([]);
        setCircuitNote("Circuit breaker detail requires API authentication (Read) — sign in or use DevelopmentBypass with a valid scope.");
      } else if (healthRes.ok) {
        const h = (await healthRes.json()) as HealthDetailedResponse;
        const cb = findCircuitBreakersEntry(h.entries);
        if (cb !== null) {
          setCircuitGates(parseCircuitGatesFromHealthEntry(cb.data as Record<string, unknown> | null | undefined));
        } else {
          setCircuitGates([]);
        }
      } else {
        setCircuitGates([]);
        setCircuitNote(`Full health report unavailable (HTTP ${healthRes.status}).`);
      }

      if (ratesRes.ok) {
        setRates((await ratesRes.json()) as OperatorTaskSuccessRatesResponse);
      } else {
        setRates(null);
        setRatesNote("Endpoint not yet available or not authorized — onboarding metrics require a successful `ReadAuthority` session.");
      }
    } catch (e) {
      setReadyError(e instanceof Error ? e.message : String(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  const overall = ready?.status ?? "Unknown";

  return (
    <main className="mx-auto max-w-3xl space-y-6" data-testid="admin-health-page">
      <div>
        <div className="flex flex-wrap items-center gap-3">
          <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">System health</h1>
          <ContextualHelp helpKey="system-health" />
          <span className={largeOverallClass(overall)} data-testid="admin-health-overall-badge">
            {overall}
          </span>
        </div>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">API readiness, circuit breakers, and in-process onboarding counters for this deployment.</p>
        <div className="mt-3">
          <Button type="button" variant="outline" size="sm" data-testid="admin-health-refresh" disabled={loading} onClick={() => void refresh()}>
            {loading ? "Refreshing…" : "Refresh"}
          </Button>
        </div>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Readiness checks</CardTitle>
          <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">GET /health/ready (anonymous) — dependency probes used by load balancers before taking traffic.</p>
        </CardHeader>
        <CardContent>
          {readyError !== null ? (
            <p className="m-0 text-sm text-rose-800 dark:text-rose-200" role="alert">
              {readyError}
            </p>
          ) : null}
          {version !== null ? (
            <div className="mb-4 rounded-md border border-neutral-200 bg-neutral-50/80 p-3 text-sm dark:border-neutral-700 dark:bg-neutral-900/50" data-testid="admin-health-build-identity">
              <p className="m-0">
                <span className="font-medium text-neutral-800 dark:text-neutral-100">Version: </span>
                <span className="font-mono text-xs">{version.informationalVersion ?? "—"}</span>
              </p>
              <p className="m-0 mt-1">
                <span className="font-medium text-neutral-800 dark:text-neutral-100">Commit: </span>
                <span className="font-mono text-xs">{version.commitSha ?? "—"}</span>
              </p>
            </div>
          ) : null}
          {ready && ready.entries.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm" data-testid="admin-health-ready-table">
                <thead>
                  <tr className="border-b border-neutral-200 text-xs uppercase text-neutral-500 dark:border-neutral-700">
                    <th className="py-2 pr-3">Check</th>
                    <th className="py-2 pr-3">Status</th>
                    <th className="py-2 pr-3">Duration</th>
                  </tr>
                </thead>
                <tbody>
                  {ready.entries.map((e) => {
                    return (
                      <tr key={e.name} className="border-b border-neutral-100 dark:border-neutral-800">
                        <td className="py-2 pr-3 font-mono text-xs text-neutral-800 dark:text-neutral-200">{e.name}</td>
                        <td className="py-2 pr-3">
                          <span className={`inline-flex rounded border px-2 py-0.5 text-xs font-medium ${statusBadgeClass(e.status)}`}>{e.status}</span>
                        </td>
                        <td className="py-2 pr-3 text-neutral-500 dark:text-neutral-400">—</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            !readyError && <p className="m-0 text-sm text-neutral-500">No readiness entries.</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Circuit breakers</CardTitle>
          <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">From GET /health (Read) — OpenAI completion/embedding gates.</p>
        </CardHeader>
        <CardContent>
          {circuitNote !== null ? (
            <p className="m-0 text-sm text-amber-900 dark:text-amber-100" data-testid="admin-health-circuit-note">
              {circuitNote}
            </p>
          ) : null}
          {circuitGates.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm" data-testid="admin-health-circuit-table">
                <thead>
                  <tr className="border-b border-neutral-200 text-xs uppercase text-neutral-500 dark:border-neutral-700">
                    <th className="py-2 pr-3">Gate</th>
                    <th className="py-2 pr-3">State</th>
                    <th className="py-2 pr-3">Duration of break (s)</th>
                  </tr>
                </thead>
                <tbody>
                  {circuitGates.map((g) => {
                    return (
                      <tr key={g.name} className="border-b border-neutral-100 dark:border-neutral-800">
                        <td className="py-2 pr-3 font-mono text-xs text-neutral-800 dark:text-neutral-200">{g.name}</td>
                        <td className="py-2 pr-3">
                          <span className={`inline-flex rounded border px-2 py-0.5 text-xs font-medium ${statusBadgeClass(g.state)}`}>
                            {g.state}
                          </span>
                        </td>
                        <td className="py-2 pr-3 text-neutral-700 dark:text-neutral-300">
                          {g.breakDurationSeconds != null && Number.isFinite(g.breakDurationSeconds) ? String(g.breakDurationSeconds) : "—"}
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : (
            !circuitNote && <p className="m-0 text-sm text-neutral-500">No circuit gate data (OpenAI may be unwired in this process).</p>
          )}
        </CardContent>
      </Card>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Operator task success rates</CardTitle>
          <p className="m-0 text-sm text-neutral-500 dark:text-neutral-400">In-process meter snapshot (resets on API host restart).</p>
        </CardHeader>
        <CardContent>
          {ratesNote !== null ? (
            <p className="m-0 text-sm text-amber-900 dark:text-amber-100" data-testid="admin-health-rates-note">
              {ratesNote}
            </p>
          ) : null}
          {rates ? (
            <div className="space-y-3" data-testid="admin-health-rates-table">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-neutral-200 text-xs uppercase text-neutral-500 dark:border-neutral-700">
                    <th className="py-2 pr-3">Metric</th>
                    <th className="py-2 pr-3">Value</th>
                    <th className="py-2 pr-3">Notes</th>
                  </tr>
                </thead>
                <tbody>
                  <tr className="border-b border-neutral-100 dark:border-neutral-800">
                    <td className="py-2 pr-3">first_run_committed (count)</td>
                    <td className="py-2 pr-3 font-semibold">{rates.firstRunCommittedTotal}</td>
                    <td className="py-2 pr-3 text-neutral-500">Process lifetime</td>
                  </tr>
                  <tr className="border-b border-neutral-100 dark:border-neutral-800">
                    <td className="py-2 pr-3">first_session_completed (count)</td>
                    <td className="py-2 pr-3 font-semibold">{rates.firstSessionCompletedTotal}</td>
                    <td className="py-2 pr-3 text-neutral-500">Process lifetime</td>
                  </tr>
                  <tr className="border-b border-neutral-100 dark:border-neutral-800">
                    <td className="py-2 pr-3">first_run / first_session ratio</td>
                    <td className="py-2 pr-3 font-semibold">{rates.firstRunCommittedPerSessionRatio.toFixed(4)}</td>
                    <td className="py-2 pr-3 text-neutral-500">—</td>
                  </tr>
                </tbody>
              </table>
              <p className="m-0 text-xs text-neutral-500 dark:text-neutral-400">{rates.windowNote}</p>
            </div>
          ) : (
            !ratesNote && <p className="m-0 text-sm text-neutral-500">(Endpoint not yet available)</p>
          )}
        </CardContent>
      </Card>
    </main>
  );
}
