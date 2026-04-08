"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { AuditEvent } from "@/lib/api";
import { getAuditEventTypes, searchAuditEvents } from "@/lib/api";

function formatUtc(iso: string): string {
  try {
    const d = new Date(iso);

    return d.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "medium" });
  } catch {
    return iso;
  }
}

function tryFormatDataJson(dataJson: string): string {
  try {
    const parsed: unknown = JSON.parse(dataJson);

    return JSON.stringify(parsed, null, 2);
  } catch {
    return dataJson;
  }
}

export default function AuditPage() {
  const [eventTypes, setEventTypes] = useState<string[]>([]);
  const [eventType, setEventType] = useState<string>("");
  const [fromUtc, setFromUtc] = useState<string>("");
  const [toUtc, setToUtc] = useState<string>("");
  const [correlationId, setCorrelationId] = useState<string>("");
  const [actorUserId, setActorUserId] = useState<string>("");
  const [runId, setRunId] = useState<string>("");
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [loadingTypes, setLoadingTypes] = useState(true);
  const [searching, setSearching] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const loadTypes = useCallback(async () => {
    setLoadingTypes(true);
    setFailure(null);
    try {
      const types = await getAuditEventTypes();
      setEventTypes(types);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoadingTypes(false);
    }
  }, []);

  useEffect(() => {
    void loadTypes();
  }, [loadTypes]);

  const runSearch = useCallback(async () => {
    setSearching(true);
    setFailure(null);
    try {
      const data = await searchAuditEvents({
        eventType: eventType || undefined,
        fromUtc: fromUtc ? new Date(fromUtc).toISOString() : undefined,
        toUtc: toUtc ? new Date(toUtc).toISOString() : undefined,
        correlationId: correlationId.trim() || undefined,
        actorUserId: actorUserId.trim() || undefined,
        runId: runId.trim() || undefined,
        take: 200,
      });
      setEvents(data);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSearching(false);
    }
  }, [actorUserId, correlationId, eventType, fromUtc, runId, toUtc]);

  function clearFilters() {
    setEventType("");
    setFromUtc("");
    setToUtc("");
    setCorrelationId("");
    setActorUserId("");
    setRunId("");
    setEvents([]);
    setFailure(null);
  }

  return (
    <main style={{ maxWidth: 900 }}>
      <h2 style={{ marginTop: 0 }}>Audit log</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Search durable audit events for the current tenant, workspace, and project. Results are newest first (up to 200
        rows per search).
      </p>

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <section
        style={{
          border: "1px solid #ddd",
          borderRadius: 8,
          padding: 12,
          marginBottom: 16,
          background: "#fff",
        }}
      >
        <div style={{ display: "grid", gap: 10, gridTemplateColumns: "repeat(auto-fill, minmax(220px, 1fr))" }}>
          <label>
            Event type{" "}
            <select
              value={eventType}
              onChange={(e) => setEventType(e.target.value)}
              disabled={loadingTypes}
              style={{ width: "100%", marginTop: 4 }}
            >
              <option value="">Any</option>
              {eventTypes.map((t) => (
                <option key={t} value={t}>
                  {t}
                </option>
              ))}
            </select>
          </label>
          <label>
            From (local){" "}
            <input
              type="datetime-local"
              value={fromUtc}
              onChange={(e) => setFromUtc(e.target.value)}
              style={{ width: "100%", marginTop: 4 }}
            />
          </label>
          <label>
            To (local){" "}
            <input
              type="datetime-local"
              value={toUtc}
              onChange={(e) => setToUtc(e.target.value)}
              style={{ width: "100%", marginTop: 4 }}
            />
          </label>
          <label>
            Correlation ID{" "}
            <input
              value={correlationId}
              onChange={(e) => setCorrelationId(e.target.value)}
              style={{ width: "100%", marginTop: 4 }}
            />
          </label>
          <label>
            Actor user id{" "}
            <input
              value={actorUserId}
              onChange={(e) => setActorUserId(e.target.value)}
              style={{ width: "100%", marginTop: 4 }}
            />
          </label>
          <label>
            Run ID{" "}
            <input
              value={runId}
              onChange={(e) => setRunId(e.target.value)}
              style={{ width: "100%", marginTop: 4 }}
            />
          </label>
        </div>
        <div style={{ marginTop: 12, display: "flex", gap: 8, flexWrap: "wrap" }}>
          <button type="button" onClick={() => void runSearch()} disabled={searching || loadingTypes}>
            {searching ? "Searching…" : "Search"}
          </button>
          <button type="button" onClick={() => clearFilters()} disabled={searching}>
            Clear filters
          </button>
        </div>
      </section>

      <p style={{ color: "#555", fontSize: 14 }}>
        {events.length} result{events.length === 1 ? "" : "s"}
      </p>

      <div style={{ display: "grid", gap: 12 }}>
        {events.length === 0 ? (
          <p style={{ color: "#666" }}>No audit events match your filters.</p>
        ) : (
          events.map((ev) => (
            <div
              key={ev.eventId}
              style={{
                border: "1px solid #ddd",
                borderRadius: 8,
                padding: 12,
                background: "#fff",
              }}
            >
              <div style={{ display: "flex", flexWrap: "wrap", gap: 8, alignItems: "center" }}>
                <strong>{formatUtc(ev.occurredUtc)}</strong>
                <span
                  style={{
                    fontSize: 12,
                    padding: "2px 8px",
                    borderRadius: 999,
                    background: "#e0e7ff",
                    color: "#312e81",
                  }}
                >
                  {ev.eventType}
                </span>
              </div>
              <div style={{ marginTop: 6, fontSize: 14 }}>
                Actor: {ev.actorUserName} ({ev.actorUserId})
              </div>
              <div style={{ fontSize: 14 }}>Correlation: {ev.correlationId ?? "—"}</div>
              <div style={{ fontSize: 14 }}>
                Run:{" "}
                {ev.runId ? (
                  <Link href={`/runs/${ev.runId}`} title="Open run detail">
                    {ev.runId}
                  </Link>
                ) : (
                  "—"
                )}
              </div>
              <details style={{ marginTop: 10 }}>
                <summary style={{ cursor: "pointer" }}>Data JSON</summary>
                <pre
                  style={{
                    marginTop: 8,
                    padding: 8,
                    background: "#f8fafc",
                    borderRadius: 6,
                    overflow: "auto",
                    fontSize: 12,
                  }}
                >
                  {tryFormatDataJson(ev.dataJson)}
                </pre>
              </details>
            </div>
          ))
        )}
      </div>
    </main>
  );
}
