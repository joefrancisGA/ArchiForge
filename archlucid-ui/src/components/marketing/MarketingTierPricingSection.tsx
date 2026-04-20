"use client";

import Link from "next/link";
import { useEffect, useState } from "react";

import { Button } from "@/components/ui/button";
import type { PricingDoc } from "@/lib/pricing-types";

function formatMoney(amount: number, currency: string): string {
  return new Intl.NumberFormat(undefined, {
    style: "currency",
    currency,
    maximumFractionDigits: 0,
  }).format(amount);
}

export type MarketingTierPricingSectionProps = {
  /** Element id for the section heading (accessibility). */
  sectionHeadingId: string;
  /** Visible section title. */
  sectionTitle: string;
  /** Optional short intro under the title. */
  sectionIntro?: string;
  /** Primary CTA target (include UTM query string when desired). */
  signupHref: string;
  /** Visible label for the primary signup CTA button. */
  signupCallToActionLabel?: string;
  /** When false, omit the trailing “Start free trial” button (e.g. welcome page already has a hero CTA). */
  showSignupCallToAction?: boolean;
};

/** Loads `/pricing.json` and renders tier cards — shared by welcome and `/pricing`. */
export function MarketingTierPricingSection(props: MarketingTierPricingSectionProps) {
  const [pricing, setPricing] = useState<PricingDoc | null>(null);
  const [pricingError, setPricingError] = useState(false);

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      try {
        const res = await fetch("/pricing.json", { cache: "no-store" });

        if (!res.ok) {
          throw new Error(String(res.status));
        }

        const json = (await res.json()) as PricingDoc;

        if (!cancelled) {
          setPricing(json);
        }
      } catch {
        if (!cancelled) {
          setPricingError(true);
        }
      }
    })();

    return () => {
      cancelled = true;
    };
  }, []);

  return (
    <section aria-labelledby={props.sectionHeadingId} className="mb-10">
      <h2 id={props.sectionHeadingId} className="mb-2 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">
        {props.sectionTitle}
      </h2>
      {props.sectionIntro ? (
        <p className="mb-6 max-w-3xl text-sm text-neutral-600 dark:text-neutral-400">{props.sectionIntro}</p>
      ) : null}

      {pricingError ? (
        <p className="text-sm text-red-600" role="alert">
          Pricing data is temporarily unavailable.
        </p>
      ) : null}

      {pricing && !pricingError ? (
        <>
          <ul className="grid gap-6 md:grid-cols-3">
            {pricing.packages.map((pkg) => (
              <li
                key={pkg.id}
                className="flex flex-col rounded-lg border border-neutral-200 bg-white p-5 shadow-sm dark:border-neutral-800 dark:bg-neutral-900"
              >
                <h3 className="text-lg font-semibold text-neutral-900 dark:text-neutral-100">{pkg.title}</h3>
                <p className="mt-2 flex-1 text-sm text-neutral-700 dark:text-neutral-300">{pkg.summary}</p>
                <dl className="mt-4 space-y-1 text-sm text-neutral-800 dark:text-neutral-200">
                  {typeof pkg.workspaceMonthlyUsd === "number" ? (
                    <div className="flex justify-between gap-2">
                      <dt>Workspace</dt>
                      <dd>{formatMoney(pkg.workspaceMonthlyUsd, pricing.currency)} / mo</dd>
                    </div>
                  ) : null}
                  {typeof pkg.seatMonthlyUsd === "number" ? (
                    <div className="flex justify-between gap-2">
                      <dt>Seat</dt>
                      <dd>{formatMoney(pkg.seatMonthlyUsd, pricing.currency)} / mo</dd>
                    </div>
                  ) : null}
                  {typeof pkg.annualFloorUsd === "number" ? (
                    <div className="flex justify-between gap-2">
                      <dt>Annual from</dt>
                      <dd>{formatMoney(pkg.annualFloorUsd, pricing.currency)}</dd>
                    </div>
                  ) : null}
                </dl>
                {pkg.id === "team" &&
                typeof pricing.teamStripeCheckoutUrl === "string" &&
                pricing.teamStripeCheckoutUrl.trim().length > 0 ? (
                  <div className="mt-4">
                    <Button asChild className="w-full" variant="outline">
                      <a href={pricing.teamStripeCheckoutUrl.trim()} rel="noopener noreferrer" target="_blank">
                        Subscribe with Stripe
                      </a>
                    </Button>
                  </div>
                ) : null}
              </li>
            ))}
          </ul>
          {props.showSignupCallToAction !== false ? (
            <div className="mt-8 flex justify-center">
              <Button
                asChild
                className="bg-teal-700 text-white hover:bg-teal-800 dark:bg-teal-800 dark:hover:bg-teal-700"
                size="lg"
              >
                <Link href={props.signupHref}>{props.signupCallToActionLabel ?? "Start free trial"}</Link>
              </Button>
            </div>
          ) : null}
        </>
      ) : null}
    </section>
  );
}
