import { Suspense } from "react";

import { DigestsHubClient } from "@/components/digests/DigestsHubClient";

export default function DigestsPage() {
  return (
    <Suspense
      fallback={
        <p className="p-4 text-sm text-neutral-500 dark:text-neutral-400" data-testid="digests-hub-suspense-fallback">
          Loading digests…
        </p>
      }
    >
      <DigestsHubClient />
    </Suspense>
  );
}
