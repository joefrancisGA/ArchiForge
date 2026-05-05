/** A subscription that routes fired alerts to a delivery channel (email, Slack, webhook, etc.). */
export type AlertRoutingSubscription = {
  routingSubscriptionId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  name: string;
  channelType: string;
  destination: string;
  minimumSeverity: string;
  isEnabled: boolean;
  createdUtc: string;
  lastDeliveredUtc?: string | null;
  metadataJson: string;
};

/** Result of a synthetic ping dispatched to a webhook routing subscription destination. */
export type WebhookTestResponse = {
  transportSucceeded: boolean;
  statusCode: number;
  reasonPhrase?: string | null;
  responseBodyPreview?: string | null;
  responseBodyTruncated: boolean;
  error?: string | null;
};

/** Record of a single attempt to deliver an alert to a routing subscription channel. */
export type AlertRoutingDeliveryAttempt = {
  alertDeliveryAttemptId: string;
  alertId: string;
  routingSubscriptionId: string;
  attemptedUtc: string;
  status: string;
  errorMessage?: string | null;
  channelType: string;
  destination: string;
  retryCount: number;
};
