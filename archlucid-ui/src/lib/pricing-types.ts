/** Shape of `public/pricing.json` (generated from `PRICING_PHILOSOPHY.md`). */
export type PricingPackage = {
  id: string;
  title: string;
  summary: string;
  workspaceMonthlyUsd?: number;
  maxWorkspaces?: number;
  includedArchitectSeats?: number;
  seatMonthlyUsd?: number;
  includedRunsPerMonth?: number;
  overageRunUsd?: number;
  annualFloorUsd?: number;
  annualCeilingUsd?: number;
};

export type PricingDoc = {
  schemaVersion: number;
  effectiveDate: string;
  currency: string;
  /** When set, Team tier card shows an external Stripe Checkout / Payment Link CTA. */
  teamStripeCheckoutUrl?: string | null;
  packages: PricingPackage[];
};
