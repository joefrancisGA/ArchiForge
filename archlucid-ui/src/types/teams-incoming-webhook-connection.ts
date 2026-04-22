export type TeamsIncomingWebhookConnectionResponse = {
  tenantId: string;
  isConfigured: boolean;
  label: string | null;
  keyVaultSecretName: string | null;
  /**
   * Per-trigger opt-in matrix returned by the API. Always non-null; for an unconfigured tenant
   * this is the v1 catalog default (all-on), for a configured tenant it is the persisted subset
   * filtered to known catalog entries and ordered canonically.
   */
  enabledTriggers: string[];
  updatedUtc: string;
};

export type TeamsIncomingWebhookConnectionUpsertRequest = {
  keyVaultSecretName: string;
  label?: string | null;
  /**
   * Per-trigger opt-in subset of the canonical catalog. Omit / `undefined` = leave existing
   * triggers unchanged (or all-on for a fresh row). Empty array = explicit opt-out of every
   * trigger. Unknown trigger names cause an HTTP 400.
   */
  enabledTriggers?: string[];
};
