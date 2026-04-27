import type { Metadata } from "next";

import { HelpDocsClient } from "./HelpDocsClient";
import { HelpProductGuide } from "./HelpProductGuide";

export const metadata: Metadata = {
  title: "Help",
};

/** Product guidance (static) + optional searchable documentation index. */
export default function HelpPage() {
  return (
    <main className="space-y-8">
      <div>
        <h1 className="m-0 text-2xl font-semibold text-neutral-900 dark:text-neutral-100">Help</h1>
        <p className="mt-2 max-w-prose text-sm text-neutral-600 dark:text-neutral-400">
          Guides for architects using ArchLucid. Search below for a curated map of in-repo reference topics (links may open
          on GitHub).
        </p>
      </div>
      <HelpProductGuide />
      <div className="space-y-2">
        <h2 className="m-0 text-base font-semibold text-neutral-900 dark:text-neutral-100">Reference documentation</h2>
        <HelpDocsClient />
      </div>
    </main>
  );
}
