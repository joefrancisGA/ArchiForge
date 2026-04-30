"use client";

import dynamic from "next/dynamic";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";

import { AskRunIdPicker } from "@/components/AskRunIdPicker";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorPageHeader } from "@/components/OperatorPageHeader";
import { EmptyState } from "@/components/EmptyState";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorLoadingNotice, OperatorMalformedCallout, OperatorTryNext } from "@/components/OperatorShellMessage";
import { GRAPH_IDLE } from "@/lib/empty-state-presets";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { isNextPublicDemoMode } from "@/lib/demo-ui-env";
import { coerceGraphViewModel } from "@/lib/operator-response-guards";
import {
  getArchitectureGraph,
  getDecisionSubgraph,
  getNodeNeighborhood,
  getProvenanceGraph,
  mergeArchitectureGraphPages,
} from "@/lib/graph-api";
import { isApiRequestError } from "@/lib/api-request-error";
import { tryStaticDemoProvenanceGraph } from "@/lib/operator-static-demo";
import { provenanceLinkageToGraphViewModel } from "@/lib/provenance-linkage-to-graph-vm";
import type { GraphViewModel } from "@/types/graph";

const GraphViewer = dynamic(
  () => import("@/components/GraphViewer").then((m) => m.GraphViewer),
  {
    ssr: false,
    loading: () => (
      <OperatorLoadingNotice>
        <strong>Loading graph viewer.</strong>
        <p className="mt-2 text-sm">Preparing the interactive canvas (client-only bundle)…</p>
      </OperatorLoadingNotice>
    ),
  },
);

/** Graph visualization mode: which endpoint to query and what graph subset to display. */
type GraphMode =
  | "provenance-full"
  | "decision-subgraph"
  | "node-neighborhood"
  | "architecture";

/** Interactive graph viewer page. Operator picks a run, graph mode, and optional filters. */
export default function GraphPage() {
  const [runId, setRunId] = useState("");
  const [decisionId, setDecisionId] = useState("");
  const [nodeId, setNodeId] = useState("");
  const [depth, setDepth] = useState(1);
  const [mode, setMode] = useState<GraphMode>("provenance-full");
  const [graph, setGraph] = useState<GraphViewModel | null>(null);
  const [loadFailure, setLoadFailure] = useState<ApiLoadFailureState | null>(null);
  const [malformedMessage, setMalformedMessage] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [typeFilter, setTypeFilter] = useState("");
  const [architectureGraphNote, setArchitectureGraphNote] = useState<string | null>(null);

  const loadGenRef = useRef(0);

  const nodeTypes = useMemo(() => {
    if (!graph) {
      return [];
    }

    const set = new Set(graph.nodes.map((n) => n.type));

    return [...set].sort((a, b) => a.localeCompare(b));
  }, [graph]);

  const performGraphLoad = useCallback(async () => {
    const gen = ++loadGenRef.current;
    setLoading(true);
    setLoadFailure(null);
    setMalformedMessage(null);
    setArchitectureGraphNote(null);

    const tryStaticProvenance = (): void => {
      if (gen !== loadGenRef.current) {
        return;
      }

      if (mode !== "provenance-full") {
        return;
      }

      const rid = runId.trim();
      const prov = tryStaticDemoProvenanceGraph(rid);

      if (prov === null) {
        return;
      }

      setLoadFailure(null);
      setMalformedMessage(null);
      setGraph(provenanceLinkageToGraphViewModel(prov));
      setTypeFilter("");
    };

    try {
      let raw: unknown;

      switch (mode) {
        case "provenance-full":
          raw = await getProvenanceGraph(runId);
          break;
        case "decision-subgraph":
          raw = await getDecisionSubgraph(runId, decisionId);
          break;
        case "node-neighborhood":
          raw = await getNodeNeighborhood(runId, nodeId, depth);
          break;
        case "architecture":
          try {
            raw = await getArchitectureGraph(runId);
          } catch (err) {
            const rid = runId.trim();

            if (
              !isApiRequestError(err) ||
              err.httpStatus !== 413 ||
              rid.length === 0
            )
              throw err;

            raw = await mergeArchitectureGraphPages(rid);
            setArchitectureGraphNote(
              "Full graph response exceeded the API size limit; loaded all pages via the paginated endpoint. Edges appear only when both endpoints fall on the same page — some cross-page links may be missing from this view.",
            );
          }
          break;
        default:
          throw new Error("Unsupported graph mode.");
      }

      const coerced = coerceGraphViewModel(raw);

      if (!coerced.ok) {
        if (gen !== loadGenRef.current) {
          return;
        }

        setGraph(null);
        setMalformedMessage(coerced.message);
        tryStaticProvenance();

        return;
      }

      if (gen !== loadGenRef.current) {
        return;
      }

      setGraph(coerced.value);
      setTypeFilter("");
      if (mode !== "architecture") {
        setArchitectureGraphNote(null);
      }
    } catch (err) {
      if (gen !== loadGenRef.current) {
        return;
      }

      setLoadFailure(toApiLoadFailure(err));
      setGraph(null);
      tryStaticProvenance();
    } finally {
      if (gen === loadGenRef.current) {
        setLoading(false);
      }
    }
  }, [mode, runId, decisionId, nodeId, depth]);

  const performRef = useRef(performGraphLoad);
  performRef.current = performGraphLoad;

  useEffect(() => {
    const rid = runId.trim();

    if (rid.length === 0) {
      return;
    }

    if (mode !== "provenance-full") {
      return;
    }

    void performRef.current();
  }, [runId, mode]);

  const showIdleCard =
    !graph && !loading && loadFailure === null && malformedMessage === null;

  const demoUi = isNextPublicDemoMode();

  const graphIdlePreset = useMemo(() => {
    if (demoUi && showIdleCard) {
      return {
        ...GRAPH_IDLE,
        title: "Architecture graph",
        description:
          "Select a run above. If no graph appears after a moment, choose **Load graph** or switch graph mode — the Claims Intake sample can provide a fallback preview when you are evaluating the product.",
      };
    }

    return GRAPH_IDLE;
  }, [demoUi, showIdleCard]);

  return (
    <main>
      <LayerHeader pageKey="graph" />
      <OperatorPageHeader title="Architecture graph" helpKey="architecture-graph" />
      <p className="max-w-3xl text-neutral-700 dark:text-neutral-300 leading-relaxed">
        Pick a run from the list, choose a graph mode, then <strong>Load graph</strong>. In demo mode, the review trail
        view can fall back to a sample graph when the API has no graph bundle yet. Node types include decisions,
        findings, artifacts, review events, and architecture components.
      </p>

      <div className="grid gap-3 max-w-4xl mb-6">
        <AskRunIdPicker
          value={runId}
          onChange={setRunId}
          selectedThreadId=""
          fieldId="graph-run"
          label="Run"
        />

        <div>
          <label htmlFor="graph-mode-select" className="block mb-1.5 text-[13px] font-semibold">
            Graph mode
          </label>
          <select
            id="graph-mode-select"
            value={mode}
            onChange={(e) => setMode(e.target.value as GraphMode)}
            className="p-2 w-full max-w-[420px]"
          >
            <option value="provenance-full">Review trail graph</option>
            <option value="decision-subgraph">Decision focus</option>
            <option value="node-neighborhood">Node connections</option>
            <option value="architecture">Architecture graph</option>
          </select>
        </div>

        {mode === "decision-subgraph" && (
          <input
            value={decisionId}
            onChange={(e) => setDecisionId(e.target.value)}
            placeholder="Decision ID"
            className="p-2"
          />
        )}

        {mode === "node-neighborhood" && (
          <>
            <input
              value={nodeId}
              onChange={(e) => setNodeId(e.target.value)}
              placeholder="Node ID"
              className="p-2"
            />
            <label className="flex items-center gap-2">
              Depth
              <input
                type="number"
                min={0}
                max={10}
                value={depth}
                onChange={(e) => setDepth(Number(e.target.value))}
                className="p-2 w-20"
              />
            </label>
          </>
        )}

        <button
          type="button"
          onClick={() => void performGraphLoad()}
          disabled={
            loading ||
            !runId ||
            (mode === "decision-subgraph" && !decisionId) ||
            (mode === "node-neighborhood" && !nodeId)
          }
          className={`px-4 py-2.5 ${loading ? "cursor-wait" : "cursor-pointer"}`}
        >
          {loading ? "Loading…" : "Load graph"}
        </button>
      </div>

      {loading && (
        <OperatorLoadingNotice>
          <strong>Loading graph.</strong>
          <p className="mt-2 text-sm">
            Requesting the selected view from the API; this may take a few seconds on large runs.
          </p>
        </OperatorLoadingNotice>
      )}

      {loadFailure !== null && (
        <>
          <OperatorApiProblem
            problem={loadFailure.problem}
            fallbackMessage={loadFailure.message}
            correlationId={loadFailure.correlationId}
          />
          <OperatorTryNext>
            This is usually a network, proxy, or HTTP error from the graph endpoint—not a malformed JSON body.
            Confirm the run exists in <Link href="/runs?projectId=default">Runs</Link>, retry{" "}
            <strong>Load graph</strong>, and check the browser network tab for the failing <code>/v1/…/graph</code>{" "}
            call.
          </OperatorTryNext>
        </>
      )}

      {malformedMessage && (
        <>
          <OperatorMalformedCallout>
            <strong>Unexpected graph response shape.</strong>
            <p className="mt-2">{malformedMessage}</p>
            <p className="mt-2 text-sm">
              The call succeeded but the payload did not match the expected GraphViewModel (nodes and edges arrays).
              Check API version alignment.
            </p>
          </OperatorMalformedCallout>
          <OperatorTryNext>
            Compare <code>GET /version</code> on the API with your UI deployment. Try another run from{" "}
            <Link href="/runs?projectId=default">Runs</Link> if this run has partial graph data.
          </OperatorTryNext>
        </>
      )}

      {showIdleCard ? <EmptyState {...graphIdlePreset} /> : null}

      {architectureGraphNote && (
        <div
          className="mb-4 max-w-4xl rounded-md border border-amber-200 bg-amber-50 px-4 py-3 text-sm text-amber-950 dark:border-amber-800 dark:bg-amber-950/40 dark:text-amber-100"
          role="status"
        >
          <strong>Large graph.</strong> {architectureGraphNote}
        </div>
      )}

      {graph && (
        <>
          <div className="mb-3 flex items-center gap-3">
            <label>
              Filter by type{" "}
              <select
                value={typeFilter}
                onChange={(e) => setTypeFilter(e.target.value)}
                className="ml-2 p-1.5"
              >
                <option value="">All types</option>
                {nodeTypes.map((t) => (
                  <option key={t} value={t}>
                    {t}
                  </option>
                ))}
              </select>
            </label>
            <span className="text-neutral-500 dark:text-neutral-400 text-sm">
              {graph.nodes.length} nodes, {graph.edges.length} edges (before filter)
            </span>
          </div>
          <GraphViewer graph={graph} typeFilter={typeFilter} />
        </>
      )}
    </main>
  );
}
