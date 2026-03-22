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
