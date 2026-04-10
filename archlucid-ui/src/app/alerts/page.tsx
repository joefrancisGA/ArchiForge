"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  OperatorEmptyState,
  OperatorLoadingNotice,
  OperatorTryNext,
} from "@/components/OperatorShellMessage";
import { applyAlertAction, listAlertsPaged } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { AlertRecord } from "@/types/alerts";

const ALERTS_PAGE_SIZE = 25;

export default function AlertsPage() {
  const [alerts, setAlerts] = useState<AlertRecord[]>([]);
  const [status, setStatus] = useState<string>("Open");
  const [page, setPage] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const totalPages = Math.max(1, Math.ceil(totalCount / ALERTS_PAGE_SIZE));

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listAlertsPaged(status || null, page, ALERTS_PAGE_SIZE);
      setAlerts(data.items);
      setTotalCount(data.totalCount);
      const pages = Math.max(1, Math.ceil(data.totalCount / ALERTS_PAGE_SIZE));

      if (data.totalCount > 0 && page > pages) {
        setPage(pages);
      }
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, [status, page]);

  useEffect(() => {
    void load();
  }, [load]);

  async function act(alertId: string, action: "Acknowledge" | "Resolve" | "Suppress") {
    const comment = typeof window !== "undefined" ? window.prompt(`Optional comment for ${action}:`) ?? "" : "";
    setFailure(null);
    try {
      await applyAlertAction(alertId, action, comment);
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  return (
    <main style={{ maxWidth: 800 }}>
      <h2 style={{ marginTop: 0 }}>Alerts</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Architecture risk alerts from scheduled scans. Open + acknowledged rows dedupe new triggers with the same key.
      </p>

      {failure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
          <OperatorTryNext>
            Confirm the API and proxy are up, then click <strong>Refresh</strong>. Alerts come from scheduled scans—if
            the list should not be empty, check worker schedules and <Link href="/">Home</Link> for environment
            guidance.
          </OperatorTryNext>
        </div>
      ) : null}

      <div style={{ marginBottom: 16 }}>
        <label>
          Status filter{" "}
          <select
            value={status}
            onChange={(e) => {
              setStatus(e.target.value);
              setPage(1);
            }}
          >
            <option value="">All</option>
            <option value="Open">Open</option>
            <option value="Acknowledged">Acknowledged</option>
            <option value="Resolved">Resolved</option>
            <option value="Suppressed">Suppressed</option>
          </select>
        </label>{" "}
        <button type="button" onClick={() => void load()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </button>
      </div>

      <div style={{ display: "grid", gap: 12 }}>
        {loading && failure === null && alerts.length === 0 ? (
          <OperatorLoadingNotice>
            <strong>Loading alerts.</strong>
            <p style={{ margin: "8px 0 0", fontSize: 14 }}>
              Fetching a page for the selected status filter ({ALERTS_PAGE_SIZE} per page). Empty results after load
              means there are no matching alerts—not a silent failure.
            </p>
          </OperatorLoadingNotice>
        ) : null}

        {!loading && failure === null && alerts.length === 0 ? (
          <OperatorEmptyState title="No alerts for this filter">
            <p style={{ margin: 0, fontSize: 14 }}>
              Try <strong>All</strong> or another status, or click <strong>Refresh</strong> after a scan window. New
              alerts appear when scheduled architecture-risk checks fire and dedupe rules allow a row.
            </p>
            <p style={{ margin: "12px 0 0", fontSize: 14 }}>
              <Link href="/">Home</Link>
              {" · "}
              <Link href="/runs?projectId=default">Runs</Link>
            </p>
          </OperatorEmptyState>
        ) : null}

        {alerts.length > 0 ? (
          alerts.map((alert) => (
            <div
              key={alert.alertId}
              style={{
                border: "1px solid #ddd",
                borderRadius: 8,
                padding: 12,
                background: "#fff",
              }}
            >
              <strong>{alert.title}</strong>
              <div>Severity: {alert.severity}</div>
              <div>Category: {alert.category}</div>
              <div>Status: {alert.status}</div>
              <div>Trigger: {alert.triggerValue}</div>
              <p style={{ marginBottom: 8 }}>{alert.description}</p>

              <div style={{ display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button type="button" onClick={() => void act(alert.alertId, "Acknowledge")}>
                  Acknowledge
                </button>
                <button type="button" onClick={() => void act(alert.alertId, "Resolve")}>
                  Resolve
                </button>
                <button type="button" onClick={() => void act(alert.alertId, "Suppress")}>
                  Suppress
                </button>
              </div>
            </div>
          ))
        ) : null}

        {!loading && failure === null && totalCount > 0 ? (
          <nav
            style={{ marginTop: 16, display: "flex", gap: 16, alignItems: "center", flexWrap: "wrap" }}
            aria-label="Alerts pagination"
          >
            <span style={{ color: "#475569", fontSize: 14 }}>
              Page {page} of {totalPages} · {totalCount} alert{totalCount === 1 ? "" : "s"} total
            </span>
            <button type="button" disabled={page <= 1} onClick={() => setPage((p) => Math.max(1, p - 1))}>
              Previous
            </button>
            <button
              type="button"
              disabled={page >= totalPages}
              onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
            >
              Next
            </button>
          </nav>
        ) : null}
      </div>
    </main>
  );
}
