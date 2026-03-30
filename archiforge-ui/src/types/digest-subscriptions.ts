/** A subscription that delivers architecture digests via a channel (email, webhook, etc.). */
export type DigestSubscription = {
  subscriptionId: string;
  tenantId: string;
  workspaceId: string;
  projectId: string;
  name: string;
  channelType: string;
  destination: string;
  isEnabled: boolean;
  createdUtc: string;
  lastDeliveredUtc?: string | null;
  metadataJson: string;
};

/** Record of a single attempt to deliver a digest to a subscription channel. */
export type DigestDeliveryAttempt = {
  attemptId: string;
  digestId: string;
  subscriptionId: string;
  attemptedUtc: string;
  status: string;
  errorMessage?: string | null;
  channelType: string;
  destination: string;
};
