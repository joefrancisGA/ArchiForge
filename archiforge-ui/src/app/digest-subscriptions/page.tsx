"use client";

import { useCallback, useEffect, useState } from "react";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  createDigestSubscription,
  listDigestSubscriptions,
  listSubscriptionDeliveryAttempts,
  toggleDigestSubscription,
} from "@/lib/api";
import type { DigestDeliveryAttempt, DigestSubscription } from "@/types/digest-subscriptions";

export default function DigestSubscriptionsPage() {
  const [items, setItems] = useState<DigestSubscription[]>([]);
  const [attemptsBySub, setAttemptsBySub] = useState<Record<string, DigestDeliveryAttempt[]>>({});
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Digest Subscription");
  const [channelType, setChannelType] = useState("Email");
  const [destination, setDestination] = useState("");

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listDigestSubscriptions();
      setItems(data);
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  async function onCreate() {
    if (!destination.trim()) return;
    setFailure(null);
    try {
      await createDigestSubscription({
        name: name.trim() || "Digest Subscription",
        channelType,
        destination: destination.trim(),
        isEnabled: true,
        metadataJson: "{}",
      });
      setDestination("");
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  async function onToggle(subscriptionId: string) {
    setFailure(null);
    try {
      await toggleDigestSubscription(subscriptionId);
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  async function loadAttempts(subscriptionId: string) {
    try {
      const rows = await listSubscriptionDeliveryAttempts(subscriptionId, 30);
      setAttemptsBySub((prev) => ({ ...prev, [subscriptionId]: rows }));
    } catch {
      /* ignore */
    }
  }

  return (
    <main style={{ maxWidth: 800 }}>
      <h2 style={{ marginTop: 0 }}>Digest subscriptions</h2>
      <p style={{ color: "#444", fontSize: 14 }}>
        When an architecture digest is generated (scheduled or manual scan), enabled subscriptions in this scope receive
        a delivery attempt. Dev uses fake email/webhook loggers — check API logs for output.
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

      <div style={{ display: "grid", gap: 12, maxWidth: 700, marginBottom: 24 }}>
        <label>
          Name
          <input
            value={name}
            onChange={(e) => setName(e.target.value)}
            placeholder="Subscription name"
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          />
        </label>
        <label>
          Channel
          <select
            value={channelType}
            onChange={(e) => setChannelType(e.target.value)}
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
          >
            <option value="Email">Email</option>
            <option value="TeamsWebhook">Teams Webhook</option>
            <option value="SlackWebhook">Slack Webhook</option>
          </select>
        </label>
        <label>
          Destination (email or webhook URL)
          <input
            value={destination}
            onChange={(e) => setDestination(e.target.value)}
            placeholder="you@example.com or https://..."
            style={{ display: "block", width: "100%", padding: 8, marginTop: 4, fontFamily: "monospace" }}
          />
        </label>
        <button type="button" onClick={() => void onCreate()} disabled={!destination.trim() || loading}>
          Create subscription
        </button>
      </div>

      <div style={{ display: "flex", gap: 8, marginBottom: 16 }}>
        <button type="button" onClick={() => void load()} disabled={loading}>
          {loading ? "Loading…" : "Refresh"}
        </button>
      </div>

      <h3>Your subscriptions</h3>
      <div style={{ display: "grid", gap: 12 }}>
        {items.length === 0 ? (
          <p style={{ color: "#666" }}>None yet.</p>
        ) : (
          items.map((item) => (
            <div
              key={item.subscriptionId}
              style={{
                border: "1px solid #ddd",
                borderRadius: 8,
                padding: 12,
                background: "#fff",
              }}
            >
              <strong>{item.name}</strong>
              <div style={{ fontSize: 14, color: "#333", marginTop: 8 }}>
                <div>Channel: {item.channelType}</div>
                <div style={{ wordBreak: "break-all" }}>Destination: {item.destination}</div>
                <div>Enabled: {String(item.isEnabled)}</div>
                <div>Last delivered: {item.lastDeliveredUtc ? new Date(item.lastDeliveredUtc).toLocaleString() : "Never"}</div>
              </div>
              <div style={{ marginTop: 12, display: "flex", gap: 8, flexWrap: "wrap" }}>
                <button type="button" onClick={() => void onToggle(item.subscriptionId)}>
                  {item.isEnabled ? "Disable" : "Enable"}
                </button>
                <button type="button" onClick={() => void loadAttempts(item.subscriptionId)}>
                  Show delivery attempts
                </button>
              </div>
              {attemptsBySub[item.subscriptionId]?.length ? (
                <ul style={{ marginTop: 12, fontSize: 13, paddingLeft: 20 }}>
                  {attemptsBySub[item.subscriptionId].map((a) => (
                    <li key={a.attemptId}>
                      {a.status} — {new Date(a.attemptedUtc).toLocaleString()}
                      {a.errorMessage ? ` — ${a.errorMessage}` : null}
                    </li>
                  ))}
                </ul>
              ) : null}
            </div>
          ))
        )}
      </div>
    </main>
  );
}
