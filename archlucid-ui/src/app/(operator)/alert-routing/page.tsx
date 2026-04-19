"use client";

import { useCallback, useEffect, useState } from "react";
import { AlertOperatorToolingRankCue } from "@/components/EnterpriseControlsContextHints";
import { LayerHeader } from "@/components/LayerHeader";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { useEnterpriseMutationCapability } from "@/hooks/use-enterprise-mutation-capability";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import {
  alertRoutingCreateSubscriptionButtonLabelReaderRank,
  alertRoutingSubscriptionsEmptyOperatorLine,
  alertRoutingSubscriptionsEmptyReaderLine,
  alertToolingConfigureSectionSubline,
  enterpriseMutationControlDisabledTitle,
} from "@/lib/enterprise-controls-context-copy";
import { cn } from "@/lib/utils";
import {
  createAlertRoutingSubscription,
  listAlertRoutingDeliveryAttempts,
  listAlertRoutingSubscriptions,
  toggleAlertRoutingSubscription,
} from "@/lib/api";
import type { AlertRoutingDeliveryAttempt, AlertRoutingSubscription } from "@/types/alert-routing";

export default function AlertRoutingPage() {
  const canMutateRouting = useEnterpriseMutationCapability();
  const [items, setItems] = useState<AlertRoutingSubscription[]>([]);
  const [attemptsBySub, setAttemptsBySub] = useState<Record<string, AlertRoutingDeliveryAttempt[]>>({});
  const [loading, setLoading] = useState(false);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  const [name, setName] = useState("Alert Routing");
  const [channelType, setChannelType] = useState("Email");
  const [destination, setDestination] = useState("");
  const [minimumSeverity, setMinimumSeverity] = useState("High");

  const load = useCallback(async () => {
    setLoading(true);
    setFailure(null);
    try {
      const data = await listAlertRoutingSubscriptions();
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
    if (!canMutateRouting || !destination.trim()) return;
    setFailure(null);
    try {
      await createAlertRoutingSubscription({
        name: name.trim() || "Alert Routing",
        channelType,
        destination: destination.trim(),
        minimumSeverity,
        isEnabled: true,
      });
      setDestination("");
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  async function onToggle(id: string) {
    if (!canMutateRouting) {
      return;
    }

    setFailure(null);
    try {
      await toggleAlertRoutingSubscription(id);
      await load();
    } catch (e) {
      setFailure(toApiLoadFailure(e));
    }
  }

  async function loadAttempts(routingSubscriptionId: string) {
    try {
      const rows = await listAlertRoutingDeliveryAttempts(routingSubscriptionId, 30);
      setAttemptsBySub((prev) => ({ ...prev, [routingSubscriptionId]: rows }));
    } catch {
      /* ignore */
    }
  }

  return (
    <main style={{ maxWidth: 800 }}>
      <LayerHeader pageKey="alert-routing" />
      <h2 style={{ marginTop: 0 }}>Alert routing</h2>
      <AlertOperatorToolingRankCue />

      {failure !== null ? (
        <div role="alert">
          <OperatorApiProblem
            problem={failure.problem}
            fallbackMessage={failure.message}
            correlationId={failure.correlationId}
          />
        </div>
      ) : null}

      <div className="flex flex-col gap-6">
        <section
          className={cn("min-w-0", !canMutateRouting && "opacity-95")}
          aria-labelledby="alert-routing-current-heading"
        >
          <h3 id="alert-routing-current-heading" style={{ fontSize: "1rem", marginTop: 4, marginBottom: 8 }}>
            Current routing
          </h3>
          <button type="button" onClick={() => void load()} disabled={loading} style={{ marginBottom: 8 }}>
            {loading ? "Loading…" : "Refresh"}
          </button>
          <div style={{ display: "grid", gap: 12 }}>
            {items.length === 0 ? (
              <p style={{ color: "#666", maxWidth: "40rem", fontSize: 14 }}>
                {canMutateRouting ? alertRoutingSubscriptionsEmptyOperatorLine : alertRoutingSubscriptionsEmptyReaderLine}
              </p>
            ) : (
              items.map((item) => (
                <div
                  key={item.routingSubscriptionId}
                  style={{
                    border: "1px solid #ddd",
                    borderRadius: 8,
                    padding: 12,
                    background: "#fff",
                  }}
                >
                  <strong>{item.name}</strong>
                  <div style={{ fontSize: 14, marginTop: 8 }}>
                    <div>Channel: {item.channelType}</div>
                    <div style={{ wordBreak: "break-all" }}>Destination: {item.destination}</div>
                    <div>Minimum severity: {item.minimumSeverity}</div>
                    <div>Enabled: {String(item.isEnabled)}</div>
                    <div>
                      Last delivered: {item.lastDeliveredUtc ? new Date(item.lastDeliveredUtc).toLocaleString() : "Never"}
                    </div>
                  </div>
                  <div style={{ marginTop: 12, display: "flex", gap: 8, flexWrap: "wrap" }}>
                    <button
                      type="button"
                      onClick={() => void onToggle(item.routingSubscriptionId)}
                      disabled={!canMutateRouting}
                      title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
                      className={cn(
                        !canMutateRouting &&
                          "rounded border border-dashed border-neutral-300 bg-neutral-50 text-neutral-600 dark:border-neutral-600 dark:bg-neutral-900/40 dark:text-neutral-400",
                      )}
                    >
                      {item.isEnabled ? "Disable" : "Enable"}
                    </button>
                    <button type="button" onClick={() => void loadAttempts(item.routingSubscriptionId)}>
                      Show delivery attempts
                    </button>
                  </div>
                  {attemptsBySub[item.routingSubscriptionId]?.length ? (
                    <ul style={{ marginTop: 12, fontSize: 13, paddingLeft: 20 }}>
                      {attemptsBySub[item.routingSubscriptionId].map((a) => (
                        <li key={a.alertDeliveryAttemptId}>
                          {a.status} — alert {a.alertId.slice(0, 8)}… — {new Date(a.attemptedUtc).toLocaleString()}
                          {a.errorMessage ? ` — ${a.errorMessage}` : null}
                          {a.retryCount > 0 ? ` (retries: ${a.retryCount})` : null}
                        </li>
                      ))}
                    </ul>
                  ) : null}
                </div>
              ))
            )}
          </div>
        </section>

        <section
          className={cn("min-w-0", !canMutateRouting && "opacity-90")}
          aria-labelledby="alert-routing-change-heading"
        >
          <h3 id="alert-routing-change-heading" style={{ fontSize: "1rem", marginTop: 4, marginBottom: 8 }}>
            {canMutateRouting ? "Change configuration" : "Change configuration (operator access)"}
          </h3>
          <p style={{ color: "#64748b", fontSize: 12, maxWidth: "40rem", marginTop: 0, marginBottom: 10 }}>
            {alertToolingConfigureSectionSubline}
          </p>
          <div style={{ display: "grid", gap: 12, maxWidth: 700, marginBottom: 16 }}>
            <label>
              Name
              <input
                value={name}
                onChange={(e) => setName(e.target.value)}
                disabled={!canMutateRouting}
                title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              />
            </label>
            <label>
              Channel
              <select
                value={channelType}
                onChange={(e) => setChannelType(e.target.value)}
                disabled={!canMutateRouting}
                title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                <option value="Email">Email</option>
                <option value="TeamsWebhook">Teams Webhook</option>
                <option value="SlackWebhook">Slack Webhook</option>
                <option value="OnCallWebhook">On-Call Webhook</option>
              </select>
            </label>
            <label>
              Destination
              <input
                value={destination}
                onChange={(e) => setDestination(e.target.value)}
                placeholder="Email or webhook URL"
                disabled={!canMutateRouting}
                title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4, fontFamily: "monospace" }}
              />
            </label>
            <label>
              Minimum severity
              <select
                value={minimumSeverity}
                onChange={(e) => setMinimumSeverity(e.target.value)}
                disabled={!canMutateRouting}
                title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
                style={{ display: "block", width: "100%", padding: 8, marginTop: 4 }}
              >
                <option value="Info">Info</option>
                <option value="Warning">Warning</option>
                <option value="High">High</option>
                <option value="Critical">Critical</option>
              </select>
            </label>
            <button
              type="button"
              onClick={() => void onCreate()}
              disabled={!destination.trim() || loading || !canMutateRouting}
              title={canMutateRouting ? undefined : enterpriseMutationControlDisabledTitle}
            >
              {canMutateRouting
                ? "Create alert routing subscription"
                : alertRoutingCreateSubscriptionButtonLabelReaderRank}
            </button>
          </div>
        </section>
      </div>
    </main>
  );
}
