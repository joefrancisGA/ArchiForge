"use client";

import { useCallback, useMemo, useState } from "react";

export type ProvenanceDiagramNode = {
  id: string;
  type: string;
  name: string;
  referenceId: string;
};

export type ProvenanceDiagramEdge = {
  id: string;
  type: string;
  fromNodeId: string;
  toNodeId: string;
};

type NodeLayout = {
  id: string;
  x: number;
  y: number;
  fill: string;
  label: string;
  layer: number;
  referenceId: string;
  type: string;
};

function layerAndColor(nodeType: string): { layer: number; fill: string } {
  const t = nodeType.toLowerCase();

  if (t.includes("contextsnapshot"))
    return { layer: 0, fill: "#94a3b8" };

  if (t.includes("graphsnapshot"))
    return { layer: 0, fill: "#64748b" };

  if (t.includes("findingssnapshot"))
    return { layer: 1, fill: "#f97316" };

  if (t.includes("finding") && !t.includes("findingssnapshot"))
    return { layer: 1, fill: "#fb923c" };

  if (t.includes("decisiontrace"))
    return { layer: 2, fill: "#3b82f6" };

  if (t.includes("decision") && !t.includes("decisiontrace"))
    return { layer: 2, fill: "#60a5fa" };

  if (t.includes("goldenmanifest"))
    return { layer: 3, fill: "#22c55e" };

  if (t.includes("artifactbundle"))
    return { layer: 4, fill: "#a78bfa" };

  return { layer: 1, fill: "#cbd5e1" };
}

function truncateLabel(text: string, maxLen: number): string {
  const s = text.trim();

  if (s.length <= maxLen)
    return s;

  return `${s.slice(0, maxLen - 1)}…`;
}

const LAYER_LABELS = [
  "Context / graph snapshots",
  "Findings",
  "Decisions",
  "Manifest",
  "Artifacts",
];

type Props = {
  nodes: ProvenanceDiagramNode[];
  edges: ProvenanceDiagramEdge[];
};

export function ProvenanceGraphDiagram({ nodes, edges }: Props) {
  const [highlightId, setHighlightId] = useState<string | null>(null);

  const { layouts, width, height } = useMemo(() => {
    const layerHeight = 108;
    const paddingX = 48;
    const paddingY = 36;
    const minWidth = 640;

    if (nodes.length === 0) {
      return { layouts: [] as NodeLayout[], width: minWidth, height: 120 };
    }

    const byLayer = new Map<number, ProvenanceDiagramNode[]>();

    for (const n of nodes) {
      const { layer } = layerAndColor(n.type);
      const list = byLayer.get(layer) ?? [];
      list.push(n);
      byLayer.set(layer, list);
    }

    const maxPerLayer = Math.max(1, ...[...byLayer.values()].map((l) => l.length));
    const width = Math.max(minWidth, paddingX * 2 + maxPerLayer * 140);
    const maxLayer = Math.max(...byLayer.keys(), 0);
    const height = paddingY * 2 + (maxLayer + 1) * layerHeight;

    const layouts: NodeLayout[] = [];

    for (let layer = 0; layer <= maxLayer; layer++) {
      const layerNodes = (byLayer.get(layer) ?? []).slice().sort((a, b) => a.id.localeCompare(b.id));
      const count = layerNodes.length;
      const gap = count > 0 ? (width - paddingX * 2) / Math.max(count, 1) : width - paddingX * 2;

      layerNodes.forEach((n, i) => {
        const { fill } = layerAndColor(n.type);
        const x = paddingX + gap * (i + 0.5);
        const y = paddingY + layer * layerHeight + layerHeight / 2;

        layouts.push({
          id: n.id,
          x,
          y,
          fill,
          label: truncateLabel(n.name || n.type, 22),
          layer,
          referenceId: n.referenceId,
          type: n.type,
        });
      });
    }

    return { layouts, width, height: Math.max(height, 160) };
  }, [nodes]);

  const posById = useMemo(() => new Map(layouts.map((l) => [l.id, l])), [layouts]);

  const onNodeActivate = useCallback((id: string) => {
    setHighlightId(id);
    const el = document.getElementById(`prov-node-row-${id}`);

    if (el) {
      el.scrollIntoView({ behavior: "smooth", block: "center" });
      el.classList.add("prov-node-row--flash");
      window.setTimeout(() => el.classList.remove("prov-node-row--flash"), 1600);
    }
  }, []);

  if (nodes.length === 0) {
    return (
      <p style={{ fontSize: 14, color: "#64748b" }} aria-live="polite">
        No graph nodes to visualize.
      </p>
    );
  }

  return (
    <section aria-labelledby="prov-graph-heading" style={{ marginBottom: 24 }}>
      <h3 id="prov-graph-heading" style={{ marginTop: 0 }}>
        Provenance graph
      </h3>
      <p style={{ fontSize: 13, color: "#444", marginTop: 4 }}>
        Layered view of coordinator linkage. Click a node to scroll to its row in the table below.
      </p>
      <div style={{ overflowX: "auto", border: "1px solid #e2e8f0", borderRadius: 6, background: "#fafafa" }}>
        <svg
          width={width}
          height={height}
          viewBox={`0 0 ${width} ${height}`}
          role="img"
          aria-label="Provenance nodes and edges"
        >
          <defs>
            <marker id="prov-arrow" markerWidth="8" markerHeight="8" refX="8" refY="4" orient="auto">
              <path d="M0,0 L8,4 L0,8 Z" fill="#64748b" />
            </marker>
          </defs>
          {edges.map((e) => {
            const from = posById.get(e.fromNodeId);
            const to = posById.get(e.toNodeId);

            if (!from || !to)
              return null;

            return (
              <g key={e.id}>
                <line
                  x1={from.x}
                  y1={from.y}
                  x2={to.x}
                  y2={to.y}
                  stroke="#94a3b8"
                  strokeWidth={1.25}
                  markerEnd="url(#prov-arrow)"
                />
                <title>{`${e.type}: ${e.fromNodeId} → ${e.toNodeId}`}</title>
              </g>
            );
          })}
          {layouts.map((n) => {
            const r = highlightId === n.id ? 22 : 18;

            return (
              <g key={n.id} style={{ cursor: "pointer" }} onClick={() => onNodeActivate(n.id)}>
                <circle
                  cx={n.x}
                  cy={n.y}
                  r={r}
                  fill={n.fill}
                  stroke={highlightId === n.id ? "#0f172a" : "#fff"}
                  strokeWidth={highlightId === n.id ? 2 : 1}
                >
                  <title>{`${n.type}\n${n.referenceId}`}</title>
                </circle>
                <text
                  x={n.x}
                  y={n.y + r + 14}
                  textAnchor="middle"
                  fontSize={11}
                  fill="#334155"
                  style={{ pointerEvents: "none" }}
                >
                  {n.label}
                </text>
              </g>
            );
          })}
        </svg>
      </div>
      <div style={{ marginTop: 12, fontSize: 12, color: "#475569" }}>
        <strong>Legend</strong>
        <ul style={{ margin: "8px 0 0", paddingLeft: 18, columns: 2, gap: 8 }}>
          <li>
            <span style={{ color: "#94a3b8" }}>●</span> ContextSnapshot
          </li>
          <li>
            <span style={{ color: "#64748b" }}>●</span> GraphSnapshot
          </li>
          <li>
            <span style={{ color: "#f97316" }}>●</span> FindingsSnapshot
          </li>
          <li>
            <span style={{ color: "#fb923c" }}>●</span> Finding
          </li>
          <li>
            <span style={{ color: "#3b82f6" }}>●</span> DecisionTrace
          </li>
          <li>
            <span style={{ color: "#60a5fa" }}>●</span> Decision
          </li>
          <li>
            <span style={{ color: "#22c55e" }}>●</span> GoldenManifest
          </li>
          <li>
            <span style={{ color: "#a78bfa" }}>●</span> ArtifactBundle
          </li>
          <li>
            <span style={{ color: "#cbd5e1" }}>●</span> Other
          </li>
        </ul>
        <p style={{ margin: "8px 0 0", fontSize: 11 }}>Layers (top to bottom): {LAYER_LABELS.join(" → ")}</p>
      </div>
      <style>{`
        .prov-node-row--flash {
          outline: 2px solid #3b82f6;
          background: #eff6ff;
          transition: background 0.3s ease;
        }
      `}</style>
    </section>
  );
}
