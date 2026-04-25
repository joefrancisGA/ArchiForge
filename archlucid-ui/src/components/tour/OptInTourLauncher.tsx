"use client";

import { useCallback, useState } from "react";

import { Button } from "@/components/ui/button";
import { recordFirstTenantFunnelEvent } from "@/lib/first-tenant-funnel-telemetry";

import { OptInTour } from "./OptInTour";

/**
 * Operator-home launcher for the in-product opt-in tour. The button is the ONLY way
 * the tour opens (owner Q9 — never auto-launch). Even users who previously dismissed
 * the tour can re-open it by clicking again.
 */
export function OptInTourLauncher() {
  const [isOpen, setIsOpen] = useState(false);

  const handleOpen = useCallback(() => {
    setIsOpen(true);
    recordFirstTenantFunnelEvent("tour_opt_in");
  }, []);

  const handleClose = useCallback(() => {
    setIsOpen(false);
  }, []);

  return (
    <>
      <Button
        type="button"
        variant="outline"
        size="sm"
        onClick={handleOpen}
        data-testid="opt-in-tour-launcher"
      >
        Show me around
      </Button>
      <OptInTour isOpen={isOpen} onClose={handleClose} />
    </>
  );
}
