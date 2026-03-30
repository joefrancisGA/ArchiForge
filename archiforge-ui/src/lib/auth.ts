import { AUTH_MODE } from "@/lib/auth-config";

/** Returns true when the UI is configured for development bypass auth (no real sign-in). */
export function isDevelopmentBypass(): boolean {
  return AUTH_MODE === "development-bypass";
}
