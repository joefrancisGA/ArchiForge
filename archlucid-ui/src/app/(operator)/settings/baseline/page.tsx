import { BaselineSettingsClient } from "./BaselineSettingsClient";

/** Baseline page uses client fetch + toast; avoid static prerender at build (no API / browser context). */
export const dynamic = "force-dynamic";

export default function BaselineSettingsPage() {
  return <BaselineSettingsClient />;
}
