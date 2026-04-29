/** Query value for `?example=` on `/runs/new` — pairs with the home-page Example request panel. */
export const OPERATOR_HOME_EXAMPLE_QUERY_VALUE = "environmental-report-pdf-analysis";

/** Long-form brief shown on home and pre-filled into the new-run wizard when `example` matches the value above. */
export const OPERATOR_HOME_EXAMPLE_DESCRIPTION =
  "Create a system to analyze corporate report PDFs for alignment with environmental standards.";

/** System name prefilled on the wizard identity step when `example` matches `OPERATOR_HOME_EXAMPLE_QUERY_VALUE`. */
export const OPERATOR_HOME_EXAMPLE_SYSTEM_NAME = "EnvironmentalReportComplianceAnalysis";

/**
 * Matched case-insensitively on run descriptions (with the static demo run id) so the home ribbon can find the
 * showcase story when the runs API returns real rows instead of the static fallback.
 */
export const OPERATOR_HOME_EXAMPLE_RUN_DESCRIPTION_TOKEN = "environmental standards";
