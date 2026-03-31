"use client";

import { useCallback, useEffect, useState, type FormEvent } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  createAdvisorySchedule,
  listAdvisorySchedules,
  listScheduleExecutions,
  runAdvisoryScheduleNow,
} from "@/lib/api";
import type { AdvisoryScanExecution, AdvisoryScanSchedule } from "@/types/advisory-scheduling";

export default function AdvisorySchedulingPage() {
  const [schedules, setSchedules] = useState<AdvisoryScanSchedule[]>([]);
  const [executionsBySchedule, setExecutionsBySchedule] = useState<Record<string, AdvisoryScanExecution[]>>(
    {},
  );
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Daily Advisory Scan");
  const [cronExpression, setCronExpression] = useState("0 7 * * *");
  const [runProjectSlug, setRunProjectSlug] = useState("default");

  const refresh = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const list = await listAdvisorySchedules();
      setSchedules(list);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void refresh();
  }, [refresh]);

  async function loadExecutions(scheduleId: string) {
    try {
      const execs = await listScheduleExecutions(scheduleId, 20);
      setExecutionsBySchedule((prev) => ({ ...prev, [scheduleId]: execs }));
    } catch {
      /* ignore */
    }
  }

  async function onCreate(e: FormEvent) {
    e.preventDefault();
    setFailure(null);
    try {
      await createAdvisorySchedule({
        name: name.trim() || "Daily Advisory Scan",
        cronExpression: cronExpression.trim() || "0 7 * * *",
        runProjectSlug: runProjectSlug.trim() || "default",
        isEnabled: true,
      });
      await refresh();
    } catch (err) {
      setFailure(toApiLoadFailure(err));
    }
  }

  async function onRunNow(scheduleId: string) {
    setFailure(null);
    try {
      await runAdvisoryScheduleNow(scheduleId);
      await loadExecutions(scheduleId);
      await refresh();
    } catch (err) {
      setFailure(toApiLoadFailure(err));
    }
  }

  return (
    <main style={{ maxWidth: 960 }}>
      <h2 style={{ marginTop: 0 }}>Advisory schedules</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Background worker polls every ~5 minutes for due schedules. Use the authority <strong>project slug</strong>{" "}
        (same as Runs list, often <code>default</code>) so recent runs are discovered.
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

      <section style={{ marginBottom: 32, padding: 16, border: "1px solid #ddd", borderRadius: 8 }}>
        <h3 style={{ marginTop: 0 }}>Create schedule</h3>
        <form onSubmit={(ev) => void onCreate(ev)} style={{ display: "grid", gap: 12, maxWidth: 480 }}>
          <label>
            Name
            <input
              value={name}
              onChange={(e) => setName(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
            />
          </label>
          <label>
            Cron / preset (<code>@hourly</code>, <code>@daily</code>, <code>0 7 * * *</code>)
            <input
              value={cronExpression}
              onChange={(e) => setCronExpression(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4, fontFamily: "monospace" }}
            />
          </label>
          <label>
            Run project slug
            <input
              value={runProjectSlug}
              onChange={(e) => setRunProjectSlug(e.target.value)}
              style={{ display: "block", width: "100%", padding: 8, marginTop: 4, fontFamily: "monospace" }}
            />
          </label>
          <button type="submit" disabled={loading}>
            Create schedule
          </button>
        </form>
      </section>

      <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
        <button type="button" onClick={() => void refresh()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </button>
      </div>

      <h3>Schedules</h3>
      {schedules.length === 0 ? (
        <p style={{ color: "#666" }}>No schedules yet.</p>
      ) : (
        <ul style={{ listStyle: "none", padding: 0 }}>
          {schedules.map((s) => (
            <li
              key={s.scheduleId}
              style={{
                border: "1px solid #ddd",
                borderRadius: 8,
                padding: 16,
                marginBottom: 12,
                background: "#fff",
              }}
            >
              <strong>{s.name}</strong>
              <div style={{ fontSize: 13, color: "#555", marginTop: 8 }}>
                <div>
                  Cron: <code>{s.cronExpression}</code>
                </div>
                <div>
                  Slug: <code>{s.runProjectSlug}</code>
                </div>
                <div>Enabled: {s.isEnabled ? "yes" : "no"}</div>
                <div>Next run: {s.nextRunUtc ? new Date(s.nextRunUtc).toLocaleString() : "—"}</div>
                <div>Last run: {s.lastRunUtc ? new Date(s.lastRunUtc).toLocaleString() : "—"}</div>
              </div>
              <div style={{ marginTop: 12, display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button type="button" onClick={() => void onRunNow(s.scheduleId)}>
                  Run now
                </button>
                <button type="button" onClick={() => void loadExecutions(s.scheduleId)}>
                  Load executions
                </button>
              </div>
              {executionsBySchedule[s.scheduleId]?.length ? (
                <div style={{ marginTop: 12 }}>
                  <h4 style={{ margin: "8px 0" }}>Recent executions</h4>
                  <ul style={{ paddingLeft: 18, fontSize: 13 }}>
                    {executionsBySchedule[s.scheduleId].map((ex) => (
                      <li key={ex.executionId}>
                        {ex.status} — {new Date(ex.startedUtc).toLocaleString()}
                        {ex.errorMessage ? ` — ${ex.errorMessage}` : null}
                      </li>
                    ))}
                  </ul>
                </div>
              ) : null}
            </li>
          ))}
        </ul>
      )}
    </main>
  );
}
