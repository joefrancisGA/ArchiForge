"use client";

import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { AuditLogRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  useNavCallerAuthorityRank,
  useOperatorNavAuthority,
} from "@/components/OperatorNavAuthorityProvider";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { AuditEvent } from "@/lib/api";
import { downloadAuditExportCsv, getAuditEventTypes, searchAuditEvents } from "@/lib/api";
import {
  canExportAuditCsv,
  formatAuditSummaryHeading,
  principalRolesAllowAuditCsvExport,
} from "@/app/(operator)/audit/audit-ui-helpers";
import {
  auditExportControlDisabledTitle,
  auditSearchNoResultsOperatorLine,
  auditSearchNoResultsReaderLine,
  auditSearchSectionLeadReaderLine,
} from "@/lib/enterprise-controls-context-copy";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import { cn } from "@/lib/utils";

function formatUtc(iso: string): string {
  try {
    const d = new Date(iso);

    return d.toLocaleString(undefined, { dateStyle: "medium", timeStyle: "medium" });
  } catch {
    return iso;
  }
}

const AUDIT_PAGE_SIZE = 200;

function tryFormatDataJson(dataJson: string): string {
  try {
    const parsed: unknown = JSON.parse(dataJson);

    return JSON.stringify(parsed, null, 2);
  } catch {
    return dataJson;
  }
}

interface AuditFilterFields {
  eventType: string;
  fromUtc: string;
  toUtc: string;
  correlationId: string;
  actorUserId: string;
  runId: string;
}

export default function AuditPage() {
  const { currentPrincipal } = useOperatorNavAuthority();
  const callerAuthorityRank = useNavCallerAuthorityRank();
  const [eventTypes, setEventTypes] = useState<string[]>([]);
  const [eventType, setEventType] = useState<string>("");
  const [fromUtc, setFromUtc] = useState<string>("");
  const [toUtc, setToUtc] = useState<string>("");
  const [correlationId, setCorrelationId] = useState<string>("");
  const [actorUserId, setActorUserId] = useState<string>("");
  const [runId, setRunId] = useState<string>("");
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [hasMoreResults, setHasMoreResults] = useState(false);
  const [loadingTypes, setLoadingTypes] = useState(true);
  const [searching, setSearching] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [exporting, setExporting] = useState(false);
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

  const executeSearch = useCallback(async (filters: AuditFilterFields, appendBeforeUtc?: string) => {
    setFailure(null);
    const payload = {
      eventType: filters.eventType || undefined,
      fromUtc: filters.fromUtc ? new Date(filters.fromUtc).toISOString() : undefined,
      toUtc: filters.toUtc ? new Date(filters.toUtc).toISOString() : undefined,
      beforeUtc: appendBeforeUtc,
      correlationId: filters.correlationId.trim() || undefined,
      actorUserId: filters.actorUserId.trim() || undefined,
      runId: filters.runId.trim() || undefined,
      take: AUDIT_PAGE_SIZE,
    };

    const data = await searchAuditEvents(payload);

    return data;
  }, []);

  const currentFilters = useCallback(
    (): AuditFilterFields => ({
      eventType,
      fromUtc,
      toUtc,
      correlationId,
      actorUserId,
      runId,
    }),
    [actorUserId, correlationId, eventType, fromUtc, runId, toUtc],
  );

  const runSearch = useCallback(async () => {
    setSearching(true);
    try {
      const data = await executeSearch(currentFilters());
      setEvents(data);
      setHasMoreResults(data.length === AUDIT_PAGE_SIZE);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSearching(false);
    }
  }, [currentFilters, executeSearch]);

  const clearFiltersAndSearch = useCallback(async () => {
    setEventType("");
    setFromUtc("");
    setToUtc("");
    setCorrelationId("");
    setActorUserId("");
    setRunId("");
    setSearching(true);
    setFailure(null);
    const empty: AuditFilterFields = {
      eventType: "",
      fromUtc: "",
      toUtc: "",
      correlationId: "",
      actorUserId: "",
      runId: "",
    };
    try {
      const data = await executeSearch(empty);
      setEvents(data);
      setHasMoreResults(data.length === AUDIT_PAGE_SIZE);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setSearching(false);
    }
  }, [executeSearch]);

  const loadMore = useCallback(async () => {
    if (events.length === 0) {
      return;
    }

    const lastOccurredUtc = events[events.length - 1]?.occurredUtc;
    if (!lastOccurredUtc) {
      return;
    }

    setLoadingMore(true);
    setFailure(null);
    try {
      const more = await executeSearch(currentFilters(), lastOccurredUtc);
      setHasMoreResults(more.length === AUDIT_PAGE_SIZE);
      setEvents((prev) => {
        const seen = new Set(prev.map((e) => e.eventId));
        const merged = [...prev];
        for (const ev of more) {
          if (!seen.has(ev.eventId)) {
            merged.push(ev);
          }
        }
        return merged;
      });
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoadingMore(false);
    }
  }, [currentFilters, events, executeSearch]);

  const onExportCsv = useCallback(async () => {
    if (!canExportAuditCsv(fromUtc, toUtc) || !principalRolesAllowAuditCsvExport(currentPrincipal.roleClaimValues)) {
      return;
    }

    setExporting(true);
    setFailure(null);
    try {
      await downloadAuditExportCsv({
        fromUtcIso: new Date(fromUtc).toISOString(),
        toUtcIso: new Date(toUtc).toISOString(),
        maxRows: 10_000,
      });
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setExporting(false);
    }
  }, [currentPrincipal.roleClaimValues, fromUtc, toUtc]);

  const exportDateRangeReady = canExportAuditCsv(fromUtc, toUtc);
  const exportRoleOk = principalRolesAllowAuditCsvExport(currentPrincipal.roleClaimValues);
  const csvExportUiAllowed = exportDateRangeReady && exportRoleOk;
  const auditSearchEmptyLine =
    callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
      ? auditSearchNoResultsReaderLine
      : auditSearchNoResultsOperatorLine;

  return (
    <main style={{ maxWidth: 900 }}>
      <LayerHeader pageKey="audit" />
      <h2 style={{ marginTop: 0 }}>Audit log</h2>
      <p className="mb-1 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        Search, then scan results. Export last and reuses the same From/To window.
      </p>
      <AuditLogRankCue />

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
        aria-labelledby="audit-search-heading"
        style={{
          border: "1px solid #ddd",
          borderRadius: 8,
          padding: 12,
          marginBottom: 16,
          background: "#fff",
        }}
      >
        <h3 id="audit-search-heading" style={{ marginTop: 0, marginBottom: 12, fontSize: "1rem" }}>
          Search audit events
        </h3>
        {callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority ? (
          <p className="mb-2 max-w-prose text-xs text-neutral-500 dark:text-neutral-400">{auditSearchSectionLeadReaderLine}</p>
        ) : null}
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
          <button type="button" onClick={() => void clearFiltersAndSearch()} disabled={searching}>
            Clear filters
          </button>
        </div>
      </section>

      <section aria-labelledby="audit-results-heading">
        <h3 id="audit-results-heading" style={{ marginTop: 0, marginBottom: 8, fontSize: "1rem" }}>
          Audit results
        </h3>
        <p role="status" aria-live="polite" aria-atomic="true" style={{ color: "#555", fontSize: 14, marginTop: 0 }}>
          {formatAuditSummaryHeading(events.length, hasMoreResults)}. Newest first, {AUDIT_PAGE_SIZE} rows per request; use
          Load more for older rows.
        </p>

        <div style={{ display: "grid", gap: 12, marginTop: 12 }}>
        {events.length === 0 ? (
          <p style={{ color: "#666" }}>{auditSearchEmptyLine}</p>
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
              {ev.otelTraceId ? (
                <div style={{ fontSize: 14 }}>
                  Trace:{" "}
                  <code title={ev.otelTraceId} style={{ fontSize: 12 }}>
                    {ev.otelTraceId.slice(0, 16)}…
                  </code>
                </div>
              ) : null}
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
              {ev.runId ? (
                <div style={{ fontSize: 13, marginTop: 2 }}>
                  <Link href={`/runs/${ev.runId}#agent-traces`} style={{ fontSize: 12 }}>
                    View agent traces →
                  </Link>
                </div>
              ) : null}
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

        {events.length > 0 && hasMoreResults ? (
          <div style={{ marginTop: 16 }}>
            <button type="button" onClick={() => void loadMore()} disabled={loadingMore || searching}>
              {loadingMore ? "Loading…" : "Load more"}
            </button>
          </div>
        ) : null}
      </section>

      <section
        aria-labelledby="audit-export-heading"
        className={cn(
          /* Deemphasize write path whenever CSV is not actionable (Read tier, missing window, or non-Auditor Operator). */
          !csvExportUiAllowed && "opacity-90",
        )}
        style={{
          border: "1px solid #ddd",
          borderRadius: 8,
          padding: 12,
          marginTop: 20,
          background: "#fafafa",
        }}
      >
        <h3 id="audit-export-heading" style={{ marginTop: 0, marginBottom: 8, fontSize: "1rem" }}>
          {csvExportUiAllowed ? "Export" : "Export (restricted)"}
        </h3>
        <p style={{ color: "#64748b", fontSize: 12, maxWidth: "40rem", marginTop: 0, marginBottom: 12 }}>
          CSV export uses the From and To values from Search audit events above. Auditor or Admin on the API is required
          for download.
        </p>
        <button
          type="button"
          onClick={() => void onExportCsv()}
          disabled={!csvExportUiAllowed || exporting || searching}
          title={
            !exportDateRangeReady
              ? "Set From and To to enable export"
              : !exportRoleOk
                ? auditExportControlDisabledTitle
                : "Download CSV for the current date range"
          }
        >
          {exporting ? "Exporting…" : "Export CSV"}
        </button>
      </section>
    </main>
  );
}
