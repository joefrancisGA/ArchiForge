"use client";

import Link from "next/link";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";

/**
 * Detail shell for `/governance/policy-packs/[id]` — distinct from `/policy-packs` (registry + lifecycle).
 * Pack-level dry-run tooling will attach here once audit + persistence surfaces ship (see product backlog).
 */
export function PolicyPackDetailClient(props: { readonly policyPackId: string }) {
  const { policyPackId } = props;

  return (
    <main className="mx-auto max-w-4xl p-6">
      <OperatorEmptyState title="Policy pack detail">
        <p className="m-0 text-sm leading-relaxed text-neutral-700 dark:text-neutral-300">
          Governance-scoped dry-run preview for policy pack <span className="font-mono text-xs">{policyPackId}</span> is not
          wired in this build. Use the workspace <strong>Policy packs</strong> page to review registered packs, versions,
          and effective content for your scope.
        </p>
        <div className="mt-4 flex flex-wrap gap-3 text-sm font-medium">
          <Link className="text-teal-800 underline dark:text-teal-300" href="/governance">
            Approvals workflow
          </Link>
          <Link className="text-teal-800 underline dark:text-teal-300" href="/policy-packs">
            Policy packs (registry)
          </Link>
        </div>
      </OperatorEmptyState>
    </main>
  );
}
