import { AUTH_MODE } from "@/lib/auth-config";

export function isDevelopmentBypass(): boolean {
  return AUTH_MODE === "development-bypass";
}
