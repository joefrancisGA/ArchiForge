/** Query value for `?example=` on `/reviews/new` — pairs with the home-page Example request panel. */
export const OPERATOR_HOME_EXAMPLE_QUERY_VALUE = "healthcare-claims-intake";

/** Long-form brief shown on home and pre-filled into the new-review wizard when `example` matches the value above. */
export const OPERATOR_HOME_EXAMPLE_DESCRIPTION =
  "Review the architecture for a healthcare claims intake modernization — REST ingestion tier, FHIR-aligned validation pipeline, and HIPAA-boundary data residency.";

/** System name prefilled on the wizard identity step when `example` matches `OPERATOR_HOME_EXAMPLE_QUERY_VALUE`. */
export const OPERATOR_HOME_EXAMPLE_SYSTEM_NAME = "HealthcareClaimsIntakeModernization";

/**
 * Matched case-insensitively on run descriptions (with the static demo run id) so the home ribbon can find the
 * showcase story when the reviews API returns real rows instead of the static fallback.
 */
export const OPERATOR_HOME_EXAMPLE_RUN_DESCRIPTION_TOKEN = "claims intake";
