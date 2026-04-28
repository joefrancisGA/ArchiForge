import type { Metadata } from "next";

import { HelpDocsClient } from "./HelpDocsClient";
import { HelpProductGuide } from "./HelpProductGuide";
import { HelpTabsShell } from "./HelpTabsShell";

export const metadata: Metadata = {
  title: "Help",
};

/** Product guidance (guide tab default) + documentation index tab. */
export default function HelpPage() {
  return (
    <main className="space-y-8">
      <div>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Help</h1>
        <p className="mt-2 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Start with the product guide. Open the Documentation tab for searchable reference links (including repo paths).
        </p>
      </div>

      <HelpTabsShell guide={<HelpProductGuide />} docs={<HelpDocsClient />} />
    </main>
  );
}
