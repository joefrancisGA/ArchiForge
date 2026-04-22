"use client";

import { useState } from "react";

import { Button } from "@/components/ui/button";

/** Anonymous quote request — POST `/v1/marketing/pricing/quote-request` via same-origin proxy. */
export function MarketingPricingQuotePanel() {
  const [workEmail, setWorkEmail] = useState("");
  const [companyName, setCompanyName] = useState("");
  const [tierInterest, setTierInterest] = useState("Professional");
  const [message, setMessage] = useState("");
  const [websiteUrl, setWebsiteUrl] = useState("");
  const [busy, setBusy] = useState(false);
  const [done, setDone] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function onSubmit(e: React.FormEvent): Promise<void> {
    e.preventDefault();
    setBusy(true);
    setError(null);

    try {
      const res = await fetch("/api/proxy/v1/marketing/pricing/quote-request", {
        method: "POST",
        headers: { "Content-Type": "application/json", Accept: "application/json" },
        body: JSON.stringify({
          workEmail,
          companyName,
          tierInterest,
          message,
          websiteUrl,
        }),
      });

      if (!res.ok) {
        const text = await res.text();
        throw new Error(text || res.statusText);
      }

      setDone(true);
    } catch (err: unknown) {
      setError(err instanceof Error ? err.message : "Request failed.");
    } finally {
      setBusy(false);
    }
  }

  return (
    <section
      id="pricing-quote-request"
      aria-labelledby="quote-request-heading"
      className="mt-10 rounded-md border border-neutral-200 bg-white p-4 dark:border-neutral-800 dark:bg-neutral-900"
    >
      <h2 id="quote-request-heading" className="mb-2 text-lg font-semibold text-neutral-900 dark:text-neutral-100">
        Request a quote
      </h2>
      <p className="mb-4 text-sm text-neutral-600 dark:text-neutral-400">
        Prefer email over calendar ping-pong when live checkout is not enabled for your procurement path. We respond
        asynchronously — no tenant is auto-created from this form.
      </p>
      {done ? (
        <p className="text-sm text-teal-800 dark:text-teal-200" role="status">
          Thanks — your request was received.
        </p>
      ) : (
        <form className="space-y-3" onSubmit={(ev) => void onSubmit(ev)} noValidate>
          <div className="hidden" aria-hidden="true">
            <label htmlFor="pricing-quote-website">Website</label>
            <input
              id="pricing-quote-website"
              name="websiteUrl"
              tabIndex={-1}
              autoComplete="off"
              value={websiteUrl}
              onChange={(ev) => setWebsiteUrl(ev.target.value)}
              className="w-full rounded border px-2 py-1 text-sm"
            />
          </div>
          <label className="flex flex-col gap-1 text-sm">
            <span>Work email</span>
            <input
              required
              type="email"
              autoComplete="email"
              value={workEmail}
              onChange={(ev) => setWorkEmail(ev.target.value)}
              className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-700 dark:bg-neutral-950"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>Company</span>
            <input
              required
              type="text"
              autoComplete="organization"
              value={companyName}
              onChange={(ev) => setCompanyName(ev.target.value)}
              className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-700 dark:bg-neutral-950"
            />
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>Tier interest</span>
            <select
              value={tierInterest}
              onChange={(ev) => setTierInterest(ev.target.value)}
              className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-700 dark:bg-neutral-950"
            >
              <option>Team</option>
              <option>Professional</option>
              <option>Enterprise</option>
            </select>
          </label>
          <label className="flex flex-col gap-1 text-sm">
            <span>Message (max 2000 characters)</span>
            <textarea
              required
              maxLength={2000}
              rows={4}
              value={message}
              onChange={(ev) => setMessage(ev.target.value)}
              className="rounded border border-neutral-300 bg-white px-2 py-1 dark:border-neutral-700 dark:bg-neutral-950"
            />
          </label>
          {error ? (
            <p className="text-sm text-red-600" role="alert">
              {error}
            </p>
          ) : null}
          <Button type="submit" disabled={busy}>
            {busy ? "Sending…" : "Submit quote request"}
          </Button>
        </form>
      )}
    </section>
  );
}
