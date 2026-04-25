"use client";

import Link from "next/link";
import { useSearchParams } from "next/navigation";

/**
 * Post-`POST /v1/register` handoff: Entra / External Id environments require email verification before trial
 * automation completes; DevelopmentBypass tenants may proceed immediately to onboarding.
 */
export function SignupVerifyClient() {
  const searchParams = useSearchParams();
  const email = searchParams.get("email")?.trim() ?? "";

  return (
    <div className="mx-auto max-w-lg space-y-4">
      <h1 className="text-2xl font-semibold text-neutral-900 dark:text-neutral-50">Check your email</h1>
      <p className="text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
        If your environment enforces Entra or External ID verification, finish the message in your inbox for{" "}
        {email.length > 0 ? <strong>{email}</strong> : "your work email"} before the trial workspace seeds demo data.
        In local DevelopmentBypass stacks, you can continue immediately — the API still applies the{" "}
        <strong>registration</strong> rate limit on <code className="rounded bg-neutral-100 px-1 text-xs dark:bg-neutral-800">POST /v1/register</code>.
      </p>
      <div className="flex flex-wrap gap-3 pt-2">
        <Link
          href="/getting-started?source=registration"
          className="inline-flex h-10 items-center justify-center rounded-md bg-teal-700 px-4 text-sm font-medium text-white hover:bg-teal-800"
          data-testid="signup-verify-continue-onboarding"
        >
          Continue to onboarding
        </Link>
        <Link
          href="/auth/signin"
          className="inline-flex h-10 items-center justify-center rounded-md border border-neutral-300 bg-white px-4 text-sm font-medium text-neutral-900 hover:bg-neutral-50 dark:border-neutral-600 dark:bg-neutral-900 dark:text-neutral-100 dark:hover:bg-neutral-800"
        >
          Sign in
        </Link>
        <Link href="/welcome" className="inline-flex h-10 items-center text-sm text-teal-800 underline dark:text-teal-300">
          Back to overview
        </Link>
      </div>
    </div>
  );
}
