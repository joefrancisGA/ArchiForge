/**
 * Path segments for ArchiForge API v1 (no trailing slash; prefix with "/" when building URLs).
 * Keeps UI aligned with `ArchiForge.Api.Routing.ApiV1Routes` in tests.
 */
export const ApiV1Routes = {
  policyPacks: "v1/policy-packs",
  governanceResolution: "v1/governance-resolution",
  alertRules: "v1/alert-rules",
  alerts: "v1/alerts",
  compositeAlertRules: "v1/composite-alert-rules",
  alertSimulation: "v1/alert-simulation",
  alertTuning: "v1/alert-tuning",
  alertRoutingSubscriptions: "v1/alert-routing-subscriptions",
  digestSubscriptions: "v1/digest-subscriptions",
  /** Pilot / product feedback rollups (58R). */
  productLearning: "v1/product-learning",
  /** 59R improvement themes and plans (read-only planning bridge). */
  learning: "v1/learning",
  /** 60R evolution: candidate change sets and simulation results. */
  evolution: "v1/evolution",
} as const;
