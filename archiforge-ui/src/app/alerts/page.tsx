"use client";

import { useCallback, useEffect, useState } from "react";
import { applyAlertAction, listAlerts } from "@/lib/api";
import type { AlertRecord } from "@/types/alerts";

export default function AlertsPage() {
  const [alerts, setAlerts] = useState<AlertRecord[]>([]);
  const [status, setStatus] = useState<string>("Open");
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setError(null);
    try {
      const data = await listAlerts(status || null, 100);
      setAlerts(data);
    } catch (e) {
      setError(e instanceof Error ? e.message : "Failed to load");
    } finally {
      setLoading(false);
    }
  }, [status]);

  useEffect(() => {
    void load();
  }, [load]);

  async function act(alertId: string, action: "Acknowledge" | "Resolve" | "Suppress") {
    const comment = typeof window !== "undefined" ? window.prompt(`Optional comment for ${action}:`) ?? "" : "";
    setError(null);
    try {
      await applyAlertAction(alertId, action, comment);
      await load();
    } catch (e) {
      setError(e instanceof Error ? e.message : "Action failed");
    }
  }

  return (
    <main style={{ maxWidth: 800 }}>
      <h2 style={{ marginTop: 0 }}>Alerts</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        Architecture risk alerts from scheduled scans. Open + acknowledged rows dedupe new triggers with the same key.
      </p>

      {error ? (
        <p style={{ color: "crimson" }} role="alert">
          {error}
        </p>
      ) : null}

      <div style={{ marginBottom: 16 }}>
        <label>
          Status filter{" "}
          <select value={status} onChange={(e) => setStatus(e.target.value)}>
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
        {alerts.length === 0 ? (
          <p style={{ color: "#666" }}>No alerts.</p>
        ) : (
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
        )}
      </div>
    </main>
  );
}
