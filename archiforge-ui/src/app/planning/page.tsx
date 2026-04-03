"use client";

import type { CSSProperties } from "react";
import { useCallback, useEffect, useMemo, useState } from "react";
import Link from "next/link";
import { PlanningExportReadinessNote } from "@/components/planning/PlanningExportReadinessNote";
import { PlanningPlansTable } from "@/components/planning/PlanningPlansTable";
import { PlanningSummarySection } from "@/components/planning/PlanningSummarySection";
import { PlanningThemesTable } from "@/components/planning/PlanningThemesTable";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { OperatorEmptyState, OperatorLoadingNotice } from "@/components/OperatorShellMessage";
import { fetchLearningPlanningListBundle } from "@/lib/api";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import { sortPlansForPlanningDisplay, sortThemesForPlanningDisplay } from "@/lib/planning-display-order";
import type { LearningPlanListItemResponse, LearningThemeResponse } from "@/types/learning";

const filterBanner: CSSProperties = {
  display: "flex",
  flexWrap: "wrap",
  alignItems: "center",
  gap: 12,
  padding: "10px 12px",
  marginBottom: 12,
  background: "#eff6ff",
  border: "1px solid #bfdbfe",
  borderRadius: 8,
  fontSize: 14,
};

/**
 * 59R planning list: top themes, prioritized plans, and evidence-style counts (read-only browsing).
 */
export default function PlanningPage() {
  const [summary, setSummary] = useState<Awaited<ReturnType<typeof fetchLearningPlanningListBundle>>["summary"] | null>(
    null,
  );
  const [themes, setThemes] = useState<LearningThemeResponse[]>([]);
  const [plans, setPlans] = useState<LearningPlanListItemResponse[]>([]);
  const [generatedUtc, setGeneratedUtc] = useState<string | null>(null);
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);
  const [selectedThemeId, setSelectedThemeId] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);

    try {
      const bundle = await fetchLearningPlanningListBundle({ maxThemes: 50, maxPlans: 50 });
      setSummary(bundle.summary);
      setThemes(bundle.themes.themes);
      setPlans(bundle.plans.plans);
      setGeneratedUtc(bundle.summary.generatedUtc);
      setSelectedThemeId((prev) => {
        if (prev === null) {
          return null;
        }

        const stillThere = bundle.themes.themes.some((t) => t.themeId === prev);

        return stillThere ? prev : null;
      });
    } catch (e) {
      setFailure(toApiLoadFailure(e));
      setSummary(null);
      setThemes([]);
      setPlans([]);
      setGeneratedUtc(null);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  const sortedThemes = useMemo(() => sortThemesForPlanningDisplay(themes), [themes]);
  const sortedPlans = useMemo(() => sortPlansForPlanningDisplay(plans), [plans]);

  const themeTitleById = useMemo(() => {
    const m = new Map<string, string>();
    for (const t of themes) {
      m.set(t.themeId, t.title);
    }

    return m;
  }, [themes]);

  const visiblePlans = useMemo(() => {
    if (selectedThemeId === null) {
      return sortedPlans;
    }

    return sortedPlans.filter((p) => p.themeId === selectedThemeId);
  }, [sortedPlans, selectedThemeId]);

  const selectedThemeTitle =
    selectedThemeId !== null ? themeTitleById.get(selectedThemeId) ?? selectedThemeId : null;

  const empty = summary !== null && summary.themeCount === 0 && summary.planCount === 0;

  return (
    <main style={{ maxWidth: 960 }}>
      <h2 style={{ marginTop: 0 }}>Planning</h2>
      <p style={{ color: "#475569", fontSize: 14, lineHeight: 1.55, maxWidth: 720 }}>
        Improvement themes and prioritized plans derived from pilot feedback (59R). This is a{" "}
        <strong>read-only</strong> browse view — use{" "}
        <Link href="/product-learning" style={{ color: "#1d4ed8" }}>
          Pilot feedback
        </Link>{" "}
        for rollups and triage export.
      </p>

      <div style={{ display: "flex", flexWrap: "wrap", gap: 12, alignItems: "center", margin: "16px 0 20px" }}>
        <button type="button" onClick={() => void load()} disabled={loading}>
          Refresh
        </button>
      </div>

      {loading && summary === null ? (
        <OperatorLoadingNotice>
          <strong>Loading planning data.</strong>
          <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching summary, themes, and plans from the API…</p>
        </OperatorLoadingNotice>
      ) : null}

      {loading && summary !== null ? (
        <p style={{ color: "#64748b", fontSize: 13, marginBottom: 16 }} role="status">
          Updating…
        </p>
      ) : null}

      {failure !== null ? (
        <div role="alert" style={{ marginBottom: 16 }}>
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      {empty && !loading ? (
        <OperatorEmptyState title="No themes or plans in this scope yet">
          <p style={{ margin: 0, fontSize: 14 }}>
            When 59R themes and improvement plans are persisted for the current tenant / workspace / project, they will
            appear here. Scope follows the operator shell defaults unless you configure proxy scope overrides.
          </p>
        </OperatorEmptyState>
      ) : null}

      {summary !== null ? (
        <>
          <PlanningSummarySection summary={summary} generatedUtc={generatedUtc} />

          <section style={{ marginBottom: 28 }} aria-labelledby="planning-themes-heading">
            <h3 id="planning-themes-heading" style={{ fontSize: 17, marginBottom: 4 }}>
              Top improvement themes
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Ordered by evidence signal count, then distinct runs. Use <strong>Plans</strong> to narrow the plan list
              to one theme.
            </p>
            <PlanningThemesTable
              themes={sortedThemes}
              plans={sortedPlans}
              selectedThemeId={selectedThemeId}
              onSelectThemeForPlans={(id) => setSelectedThemeId(id)}
            />
          </section>

          <section style={{ marginBottom: 24 }} aria-labelledby="planning-plans-heading">
            <h3 id="planning-plans-heading" style={{ fontSize: 17, marginBottom: 4 }}>
              Prioritized improvement plans
            </h3>
            <p style={{ color: "#64748b", fontSize: 13, marginTop: 0 }}>
              Ordered by priority score (highest first). Open a row for action steps and link-level evidence counts.
            </p>

            {selectedThemeId !== null ? (
              <div style={filterBanner} role="status">
                <span>
                  Showing plans for theme: <strong>{selectedThemeTitle}</strong> ({visiblePlans.length} of{" "}
                  {sortedPlans.length})
                </span>
                <button type="button" onClick={() => setSelectedThemeId(null)}>
                  Show all plans
                </button>
              </div>
            ) : null}

            <PlanningPlansTable plans={visiblePlans} themeTitleById={themeTitleById} />
          </section>

          <PlanningExportReadinessNote />
        </>
      ) : null}
    </main>
  );
}
