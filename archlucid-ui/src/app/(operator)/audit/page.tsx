"use client";

import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { HelpLink } from "@/components/HelpLink";
import { GlossaryTooltip } from "@/components/GlossaryTooltip";
import { AuditLogRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import {
  useNavCallerAuthorityRank,
  useOperatorNavAuthority,
} from "@/components/OperatorNavAuthorityProvider";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { AuditEvent, CursorPagedResponse } from "@/lib/api";
import { downloadAuditExportCsv, getAuditEventTypes, searchAuditEvents } from "@/lib/api";
import { cn } from "@/lib/utils";
import {
  canExportAuditCsv,
  formatAuditSummaryHeading,
  principalRolesAllowAuditCsvExport,
} from "@/app/(operator)/audit/audit-ui-helpers";
import {
  auditExportControlDisabledTitle,
  auditExportCsvButtonLabelRoleRestricted,
  auditExportCsvButtonLabelWindowIncomplete,
  auditExportExecuteRankAuditorRoleNote,
  auditExportSectionSupportingLine,
  auditClearFiltersButtonLabelReaderRank,
  auditLoadMoreButtonTitleOperator,
  auditLoadMoreButtonTitleReader,
  auditResultsSectionHeadingOperator,
  auditResultsSectionHeadingReader,
  auditSearchEventsButtonLabelReaderRank,
  auditSearchEventsButtonTitleOperator,
  auditSearchEventsButtonTitleReader,
  auditSearchEventsSectionHeadingOperator,
  auditSearchEventsSectionHeadingReader,
  auditSearchNoResultsOperatorLine,
  auditSearchNoResultsReaderLine,
  auditSearchSectionLeadReaderLine,
} from "@/lib/enterprise-controls-context-copy";
import { AUTHORITY_RANK } from "@/lib/nav-authority";
import {
  getDemoSampleAuditTrailEvents,
  shouldInjectDemoAuditSample,
} from "@/lib/demo-audit-sample-events";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { isStaticDemoPayloadFallbackEnabled, shouldMergeOperatorDemoAlertSample } from "@/lib/operator-static-demo";
import { SHOWCASE_STATIC_DEMO_RUN_ID } from "@/lib/showcase-static-demo";

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
  const canMutateEnterpriseShell = useEnterpriseMutationCapability();
  const [eventTypes, setEventTypes] = useState<string[]>([]);
  const [eventType, setEventType] = useState<string>("");
  const [fromUtc, setFromUtc] = useState<string>("");
  const [toUtc, setToUtc] = useState<string>("");
  const [correlationId, setCorrelationId] = useState<string>("");
  const [actorUserId, setActorUserId] = useState<string>("");
  const [runId, setRunId] = useState<string>(() =>
    isNextPublicDemoMode() || isStaticDemoPayloadFallbackEnabled() ? SHOWCASE_STATIC_DEMO_RUN_ID : "",
  );
  const [events, setEvents] = useState<AuditEvent[]>([]);
  const [hasMoreResults, setHasMoreResults] = useState(false);
  const [auditNextCursor, setAuditNextCursor] = useState<string | null>(null);
  const [loadingTypes, setLoadingTypes] = useState(true);
  const [searching, setSearching] = useState(false);
  const [loadingMore, setLoadingMore] = useState(false);
  const [exporting, setExporting] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const demoAuditPrimedRef = useRef(false);

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

  const executeSearch = useCallback(
    async (filters: AuditFilterFields, loadMoreCursor?: string | null) => {
      setFailure(null);

      const payload = {
        eventType: filters.eventType || undefined,
        fromUtc: filters.fromUtc ? new Date(filters.fromUtc).toISOString() : undefined,
        toUtc: filters.toUtc ? new Date(filters.toUtc).toISOString() : undefined,
        cursor: loadMoreCursor ?? undefined,
        correlationId: filters.correlationId.trim() || undefined,
        actorUserId: filters.actorUserId.trim() || undefined,
        runId: filters.runId.trim() || undefined,
        take: AUDIT_PAGE_SIZE,
      };

      const data: CursorPagedResponse<AuditEvent> = await searchAuditEvents(payload);

      return data;
    },
    [],
  );

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
      const filters = currentFilters();
      const page = await executeSearch(filters);
      const injectDemo =
        shouldMergeOperatorDemoAlertSample() && shouldInjectDemoAuditSample(filters) && page.items.length === 0;
      setEvents(injectDemo ? getDemoSampleAuditTrailEvents() : page.items);
      setHasMoreResults(injectDemo ? false : page.hasMore);
      setAuditNextCursor(injectDemo ? null : page.nextCursor);
    } catch (e) {
      const emptyFilters = currentFilters();
      const injectOnError =
        shouldMergeOperatorDemoAlertSample() && shouldInjectDemoAuditSample(emptyFilters);

      if (injectOnError) {
        setEvents(getDemoSampleAuditTrailEvents());
        setHasMoreResults(false);
        setAuditNextCursor(null);
        setFailure(null);
      } else {
        setFailure(toApiLoadFailure(e));
      }
    } finally {
      setSearching(false);
    }
  }, [currentFilters, executeSearch]);

  useEffect(() => {
    if (!shouldMergeOperatorDemoAlertSample() || demoAuditPrimedRef.current) {
      return;
    }

    demoAuditPrimedRef.current = true;
    void runSearch();
  }, [runSearch]);

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
      const page = await executeSearch(empty);
      const injectDemo =
        shouldMergeOperatorDemoAlertSample() && shouldInjectDemoAuditSample(empty) && page.items.length === 0;
      setEvents(injectDemo ? getDemoSampleAuditTrailEvents() : page.items);
      setHasMoreResults(injectDemo ? false : page.hasMore);
      setAuditNextCursor(injectDemo ? null : page.nextCursor);
    } catch (e) {
      const injectOnError = shouldMergeOperatorDemoAlertSample() && shouldInjectDemoAuditSample(empty);

      if (injectOnError) {
        setEvents(getDemoSampleAuditTrailEvents());
        setHasMoreResults(false);
        setAuditNextCursor(null);
        setFailure(null);
      } else {
        setFailure(toApiLoadFailure(e));
      }
    } finally {
      setSearching(false);
    }
  }, [executeSearch]);

  const loadMore = useCallback(async () => {
    if (events.length === 0) {
      return;
    }

    if (!auditNextCursor) {
      return;
    }

    setLoadingMore(true);
    setFailure(null);
    try {
      const page = await executeSearch(currentFilters(), auditNextCursor);
      setHasMoreResults(page.hasMore);
      setAuditNextCursor(page.nextCursor);
      setEvents((prev) => {
        const seen = new Set(prev.map((e) => e.eventId));
        const merged = [...prev];
        for (const ev of page.items) {
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
  }, [currentFilters, events.length, auditNextCursor, executeSearch]);

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
    <main className="max-w-4xl">
      <LayerHeader pageKey="audit" />
      <OperatorPageHeader
        title="Audit log"
        helpKey="audit-log"
        actions={
          <HelpLink
            docPath="/docs/library/AUDIT_COVERAGE_MATRIX.md"
            label="Audit coverage matrix documentation on GitHub (new tab)"
          />
        }
      />
      <AuditLogRankCue className="mb-2" />

      {callerAuthorityRank >= AUTHORITY_RANK.ExecuteAuthority && !exportRoleOk ? (
        <p className="mb-2 max-w-prose text-xs text-neutral-600 dark:text-neutral-400" role="note">
          {auditExportExecuteRankAuditorRoleNote}
        </p>
      ) : null}

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
        className="border border-neutral-200 dark:border-neutral-700 rounded-lg p-3 mb-4 bg-white dark:bg-neutral-950"
      >
        <h3 id="audit-search-heading" className="mt-0 mb-3 text-base">
          {callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
            ? auditSearchEventsSectionHeadingReader
            : auditSearchEventsSectionHeadingOperator}
        </h3>
        {callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority ? (
          <p className="mb-2 max-w-prose text-xs text-neutral-500 dark:text-neutral-400">{auditSearchSectionLeadReaderLine}</p>
        ) : null}
        <div className="grid gap-2.5 grid-cols-[repeat(auto-fill,minmax(220px,1fr))]">
          <label>
            Event type{" "}
            <select
              value={eventType}
              onChange={(e) => setEventType(e.target.value)}
              disabled={loadingTypes}
              className="w-full mt-1"
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
              className="w-full mt-1"
            />
          </label>
          <label>
            To (local){" "}
            <input
              type="datetime-local"
              value={toUtc}
              onChange={(e) => setToUtc(e.target.value)}
              className="w-full mt-1"
            />
          </label>
          <label>
            Correlation ID{" "}
            <input
              value={correlationId}
              onChange={(e) => setCorrelationId(e.target.value)}
              className="w-full mt-1"
            />
          </label>
          <label>
            Actor user id{" "}
            <input
              value={actorUserId}
              onChange={(e) => setActorUserId(e.target.value)}
              className="w-full mt-1"
            />
          </label>
          <label>
            Review ID{" "}
            <input
              value={runId}
              onChange={(e) => setRunId(e.target.value)}
              className="w-full mt-1"
            />
          </label>
        </div>
        <div className="mt-3 flex gap-2 flex-wrap">
          <button
            type="button"
            onClick={() => void runSearch()}
            disabled={searching || loadingTypes}
            title={
              callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
                ? auditSearchEventsButtonTitleReader
                : auditSearchEventsButtonTitleOperator
            }
          >
            {searching ? "Searching…" : canMutateEnterpriseShell ? "Search" : auditSearchEventsButtonLabelReaderRank}
          </button>
          <button
            type="button"
            onClick={() => void clearFiltersAndSearch()}
            disabled={searching}
            title={
              canMutateEnterpriseShell
                ? "Clear filter fields and run search with empty criteria"
                : "Clear fields and re-run search (GET only; export rules unchanged)"
            }
          >
            {canMutateEnterpriseShell ? "Clear filters" : auditClearFiltersButtonLabelReaderRank}
          </button>
        </div>
      </section>

      <section aria-labelledby="audit-results-heading">
        <h3 id="audit-results-heading" className="mt-0 mb-2 text-base">
          {callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
            ? auditResultsSectionHeadingReader
            : auditResultsSectionHeadingOperator}
        </h3>
        <p className="text-neutral-600 dark:text-neutral-400 text-[13px] mt-0 mb-2 max-w-2xl">
          Each card below is one <GlossaryTooltip termKey="audit_event">audit event</GlossaryTooltip> (time, type, actor,
          and run when present). Expand an event for technical detail.
        </p>
        <p role="status" aria-live="polite" aria-atomic="true" className="text-neutral-600 dark:text-neutral-400 text-sm mt-0">
          {formatAuditSummaryHeading(events.length, hasMoreResults)}. Newest first, {AUDIT_PAGE_SIZE} rows per request; use
          Load more for older rows.
        </p>

        <div className="grid gap-3 mt-3">
        {events.length === 0 ? (
          <p className="text-neutral-500 dark:text-neutral-400">{auditSearchEmptyLine}</p>
        ) : (
          events.map((ev) => (
            <div
              key={ev.eventId}
              className="border border-neutral-200 dark:border-neutral-700 rounded-lg p-3 bg-white dark:bg-neutral-950"
            >
              <div className="flex flex-wrap gap-2 items-center">
                <strong>{formatUtc(ev.occurredUtc)}</strong>
                <span
                  className="text-xs px-2 py-0.5 rounded-full bg-indigo-100 text-indigo-900 dark:bg-indigo-900/50 dark:text-indigo-300"
                >
                  {ev.eventType}
                </span>
              </div>
              <div className="mt-1.5 text-sm">
                Actor: {ev.actorUserName} ({ev.actorUserId})
              </div>
              <div className="text-sm">Correlation: {ev.correlationId ?? "—"}</div>
              {ev.otelTraceId ? (
                <div className="text-sm">
                  Trace:{" "}
                  <code title={ev.otelTraceId} className="text-xs">
                    {ev.otelTraceId.slice(0, 16)}…
                  </code>
                </div>
              ) : null}
              <div className="text-sm">
                Run:{" "}
                {ev.runId ? (
                  <Link href={`/reviews/${ev.runId}`} title="Open review">
                    {ev.runId}
                  </Link>
                ) : (
                  "—"
                )}
              </div>
              {ev.runId ? (
                <div className="text-[13px] mt-0.5">
                  <Link href={`/reviews/${ev.runId}#agent-traces`} className="text-xs">
                    View agent traces →
                  </Link>
                </div>
              ) : null}
              <details className="mt-2.5">
                <summary className="cursor-pointer">Data JSON</summary>
                <pre
                  className="mt-2 p-2 bg-neutral-50/90 dark:bg-neutral-900/50 rounded-md overflow-auto text-xs"
                >
                  {tryFormatDataJson(ev.dataJson)}
                </pre>
              </details>
            </div>
          ))
        )}
        </div>

        {events.length > 0 && hasMoreResults ? (
          <div className="mt-4">
            <button
              type="button"
              onClick={() => void loadMore()}
              disabled={loadingMore || searching}
              title={
                callerAuthorityRank < AUTHORITY_RANK.ExecuteAuthority
                  ? auditLoadMoreButtonTitleReader
                  : auditLoadMoreButtonTitleOperator
              }
            >
              {loadingMore ? "Loading…" : "Load more"}
            </button>
          </div>
        ) : null}
      </section>

      <section
        aria-labelledby="audit-export-heading"
        className={cn(
          "border border-neutral-200 dark:border-neutral-700 rounded-lg p-3 mt-5 bg-neutral-50 dark:bg-neutral-950",
          !csvExportUiAllowed && "opacity-90",
        )}
      >
        <h3 id="audit-export-heading" className="mt-0 mb-2 text-base">
          {csvExportUiAllowed ? "Export" : "Export (restricted)"}
        </h3>
        <p className="text-neutral-500 dark:text-neutral-400 text-xs max-w-xl mt-0 mb-3">
          {auditExportSectionSupportingLine}
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
          {exporting
            ? "Exporting…"
            : csvExportUiAllowed
              ? "Export CSV"
              : !exportDateRangeReady
                ? auditExportCsvButtonLabelWindowIncomplete
                : !exportRoleOk
                  ? auditExportCsvButtonLabelRoleRestricted
                  : "Export CSV"}
        </button>
      </section>
    </main>
  );
}
