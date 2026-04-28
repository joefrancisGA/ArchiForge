import type { Metadata } from "next";
import { Suspense } from "react";

import { HelpLink } from "@/components/HelpLink";
import { ContextualHelp } from "@/components/ContextualHelp";
import { NewRunWizardSkeleton } from "@/components/skeletons/NewRunWizardSkeleton";
import { NewRunWizardClient } from "./NewRunWizardClient";

export const metadata: Metadata = {
  title: "New architecture request",
};

export default function NewRunPage() {
  return (
    <main>
      <div className="mb-1 flex flex-wrap items-center gap-2">
        <h2 className="m-0">New architecture request</h2>
        <ContextualHelp helpKey="new-run-wizard" />
        <HelpLink
          docPath="/docs/library/PILOT_GUIDE.md"
          label="Full pilot guidance on GitHub (new tab)"
        />
      </div>
      <p className="mt-1 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
        Create a request from scratch, use an industry starter, or import a prepared file.
      </p>
      <Suspense fallback={<NewRunWizardSkeleton />}>
        <NewRunWizardClient />
      </Suspense>
    </main>
  );
}
