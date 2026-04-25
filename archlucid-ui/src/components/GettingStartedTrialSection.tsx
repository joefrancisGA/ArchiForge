"use client";

import { useEffect, useState } from "react";

import { OnboardingStartClient } from "@/components/OnboardingStartClient";
import { readLastRegistrationPayload } from "@/lib/registration-session";

export type GettingStartedTrialSectionProps = {
  fromRegistrationQuery: boolean;
};

/**
 * Trial status + sample run handoff: shown after signup (`?source=registration`) or while registration
 * scope is still in session (same heuristics as the former `/onboarding/start` page).
 */
export function GettingStartedTrialSection({ fromRegistrationQuery }: GettingStartedTrialSectionProps) {
  const [fromSession, setFromSession] = useState(false);

  useEffect(() => {
    if (fromRegistrationQuery) return;

    if (readLastRegistrationPayload() !== null) setFromSession(true);
  }, [fromRegistrationQuery]);

  if (!fromRegistrationQuery && !fromSession) return null;

  return (
    <div className="mb-8">
      {fromRegistrationQuery ? (
        <h2 className="mb-2 text-xl font-semibold text-neutral-900 dark:text-neutral-100">Onboarding</h2>
      ) : null}
      {fromRegistrationQuery ? (
        <p className="mb-6 max-w-3xl text-sm text-neutral-700 dark:text-neutral-300">
          Confirm trial limits below, then use the Core Pilot checklist or open the new-run wizard with the sample
          highlighted on step one.
        </p>
      ) : null}
      <OnboardingStartClient />
    </div>
  );
}
