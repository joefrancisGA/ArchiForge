/**
 * Path segments for ArchLucid API v1 (no trailing slash; prefix with "/" when building URLs).
 * Keeps UI aligned with `ArchLucid.Api.Routing.ApiV1Routes` in tests.
 */
export const ApiV1Routes = {
  policyPacks: "v1/policy-packs",
  governanceResolution: "v1/governance-resolution",
  governance: "v1/governance",
  alertRules: "v1/alert-rules",
  alerts: "v1/alerts",
  compositeAlertRules: "v1/composite-alert-rules",
  alertSimulation: "v1/alert-simulation",
  alertTuning: "v1/alert-tuning",
  alertRoutingSubscriptions: "v1/alert-routing-subscriptions",
  digestSubscriptions: "v1/digest-subscriptions",
  tenantExecDigestPreferences: "v1/tenant/exec-digest-preferences",
  tenantCostEstimate: "v1/tenant/cost-estimate",
  tenantMeasuredRoi: "v1/tenant/measured-roi",
  /** Sponsor evidence bundle (Standard tier): explainability completeness, deltas, governance counts. */
  pilotsSponsorEvidencePack: "v1/pilots/sponsor-evidence-pack",
  teamsIncomingWebhookConnections: "v1/integrations/teams/connections",
  teamsNotificationTriggerCatalog: "v1/integrations/teams/triggers",
  /** Pilot / product feedback rollups (58R). */
  productLearning: "v1/product-learning",
  /** 59R improvement themes and plans (read-only planning bridge). */
  learning: "v1/learning",
  /** 60R evolution: candidate change sets and simulation results. */
  evolution: "v1/evolution",
} as const;
