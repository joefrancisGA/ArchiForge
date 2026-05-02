import { CORE_PILOT_STEPS, type CorePilotStepBase } from "@/lib/core-pilot-steps";

export type CorePilotHelpStepContext = {
  stepIndex: number;
  step: CorePilotStepBase;
};

/**
 * Map operator routes to the closest Core Pilot checklist step for in-app Help.
 * Pathname-only: review detail cannot infer finalize state, so `/reviews/{id}` maps to the finalize step.
 */
export function corePilotHelpStepForPath(pathname: string): CorePilotHelpStepContext | null {
  const normalized = (pathname.trim().length === 0 ? "/" : pathname) || "/";

  if (normalized === "/" || normalized === "/onboarding") {
    return { stepIndex: 0, step: CORE_PILOT_STEPS[0] };
  }

  if (normalized === "/reviews/new") {
    return { stepIndex: 0, step: CORE_PILOT_STEPS[0] };
  }

  if (normalized === "/reviews") {
    return { stepIndex: 1, step: CORE_PILOT_STEPS[1] };
  }

  if (/^\/reviews\/[^/]+$/.test(normalized)) {
    return { stepIndex: 2, step: CORE_PILOT_STEPS[2] };
  }

  return null;
}
