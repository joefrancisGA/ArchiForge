import { Suspense } from "react";

import { AdvisoryHubClient } from "@/components/advisory/AdvisoryHubClient";

export default function AdvisoryPage() {
  return (
    <Suspense
      fallback={
        <p className="p-4 text-sm text-neutral-500 dark:text-neutral-400" data-testid="advisory-hub-suspense-fallback">
          Loading advisory…
        </p>
      }
    >
      <AdvisoryHubClient />
    </Suspense>
  );
}
