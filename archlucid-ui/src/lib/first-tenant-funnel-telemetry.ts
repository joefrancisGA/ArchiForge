import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

/**
 * Improvement 12 — first-tenant onboarding funnel telemetry client.
 *
 * Fire-and-forget POST to /v1/diagnostics/first-tenant-funnel. Never throws,
 * never blocks UI, ignores non-OK responses. The server infers tenantId from
 * the request scope; we never include it in the payload.
 *
 * Default emission server-side is AGGREGATED-ONLY (no per-tenant correlation).
 * The owner-only feature flag Telemetry:FirstTenantFunnel:PerTenantEmission
 * controls whether tenant-scoped rows are persisted; the UI behavior is
 * identical either way. See docs/security/PRIVACY_NOTE.md §3.A.
 */
export type FirstTenantFunnelEvent =
  | "signup"
  | "tour_opt_in"
  | "first_run_started"
  | "first_run_committed"
  | "first_finding_viewed"
  | "thirty_minute_milestone";

const ALLOWED_EVENTS: ReadonlySet<FirstTenantFunnelEvent> = new Set<FirstTenantFunnelEvent>([
  "signup",
  "tour_opt_in",
  "first_run_started",
  "first_run_committed",
  "first_finding_viewed",
  "thirty_minute_milestone",
]);

const SIGNUP_TIMESTAMP_KEY = "archlucid.firstTenantFunnel.signupUtc";
const MILESTONE_FIRED_KEY = "archlucid.firstTenantFunnel.milestoneFired";
const THIRTY_MINUTES_MS = 30 * 60 * 1000;

function isBrowser(): boolean {
  return typeof window !== "undefined";
}

function safeLocalStorageGet(key: string): string | null {
  try {
    return window.localStorage.getItem(key);
  } catch {
    return null;
  }
}

function safeLocalStorageSet(key: string, value: string): void {
  try {
    window.localStorage.setItem(key, value);
  } catch {
    /* localStorage may be disabled (private mode); silently ignore. */
  }
}

/**
 * Records the signup wall-clock timestamp so the 30-minute milestone can be
 * evaluated later in the session. Safe to call multiple times — only the
 * first value is kept so re-renders or retries don't reset the clock.
 */
function rememberSignupTimestamp(): void {
  if (!isBrowser()) return;

  const existing: string | null = safeLocalStorageGet(SIGNUP_TIMESTAMP_KEY);

  if (existing !== null && existing.length > 0) return;

  safeLocalStorageSet(SIGNUP_TIMESTAMP_KEY, new Date().toISOString());
}

/**
 * Returns true if signup happened within the last 30 minutes and the
 * milestone has not yet been fired in this browser. Side-effect free;
 * caller decides whether to emit and mark fired.
 */
function shouldFireThirtyMinuteMilestone(): boolean {
  if (!isBrowser()) return false;

  const fired: string | null = safeLocalStorageGet(MILESTONE_FIRED_KEY);

  if (fired === "1") return false;

  const signupIso: string | null = safeLocalStorageGet(SIGNUP_TIMESTAMP_KEY);

  if (signupIso === null || signupIso.length === 0) return false;

  const signupMs: number = Date.parse(signupIso);

  if (Number.isNaN(signupMs)) return false;

  const elapsedMs: number = Date.now() - signupMs;
  return elapsedMs >= 0 && elapsedMs <= THIRTY_MINUTES_MS;
}

function markMilestoneFired(): void {
  if (!isBrowser()) return;

  safeLocalStorageSet(MILESTONE_FIRED_KEY, "1");
}

function postFunnelEvent(eventName: FirstTenantFunnelEvent): void {
  if (!isBrowser()) return;

  if (!ALLOWED_EVENTS.has(eventName)) return;

  void fetch(
    "/api/proxy/v1/diagnostics/first-tenant-funnel",
    mergeRegistrationScopeForProxy({
      method: "POST",
      headers: {
        "Content-Type": "application/json",
        Accept: "application/json",
      },
      body: JSON.stringify({ event: eventName }),
      keepalive: true,
    }),
  ).catch(() => {
    /* intentional: telemetry must not surface secondary failures */
  });
}

/**
 * Records one funnel event. For "first_finding_viewed" this also opportunistically
 * fires the 30-minute milestone if the user reached this step within 30 minutes
 * of signup (per the Improvement 12 success metric).
 */
export function recordFirstTenantFunnelEvent(eventName: FirstTenantFunnelEvent): void {
  if (!isBrowser()) return;

  if (eventName === "signup") rememberSignupTimestamp();

  postFunnelEvent(eventName);

  if (eventName !== "first_finding_viewed") return;

  if (!shouldFireThirtyMinuteMilestone()) return;

  markMilestoneFired();
  postFunnelEvent("thirty_minute_milestone");
}
