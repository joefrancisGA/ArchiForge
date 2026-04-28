"use client";

import { Map } from "lucide-react";

import { Button } from "@/components/ui/button";
import { ARCHLUCID_ONBOARDING_TOUR_START_EVENT } from "@/lib/onboarding-tour";

/** Dispatches the global event {@link OnboardingTour} listens for (Help page entry point). */
export function HelpTourTrigger() {
  return (
    <Button
      type="button"
      variant="outline"
      size="sm"
      className="gap-1.5"
      onClick={() => {
        window.dispatchEvent(new CustomEvent(ARCHLUCID_ONBOARDING_TOUR_START_EVENT));
      }}
    >
      <Map className="h-3.5 w-3.5" aria-hidden />
      Take the tour
    </Button>
  );
}
