import { Suspense } from "react";

import { AlertsHubClient } from "./AlertsHubClient";

export default function AlertsPage() {
  return (
    <Suspense
      fallback={
        <p className="p-4 text-sm text-neutral-500 dark:text-neutral-400" data-testid="alerts-hub-suspense-fallback">
          Loading alerts…
        </p>
      }
    >
      <AlertsHubClient />
    </Suspense>
  );
}
