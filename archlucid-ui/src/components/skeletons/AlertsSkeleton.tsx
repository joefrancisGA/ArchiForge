import { Skeleton } from "@/components/ui/skeleton";

/**
 * Mirrors the alerts page: heading, filter row, and stacked alert cards.
 */
export function AlertsSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading alerts" className="space-y-4">
      <Skeleton className="h-7 w-32" />
      <Skeleton className="h-4 w-64" />
      <Skeleton className="h-10 w-28" />
      <div className="space-y-3">
        {[1, 2, 3, 4].map((i) => (
          <div
            key={i}
            className="rounded-lg border border-neutral-200 bg-white p-4 dark:border-neutral-700 dark:bg-neutral-900/40"
          >
            <Skeleton className="h-5 w-full max-w-md" />
            <Skeleton className="mt-3 h-4 w-full" />
            <Skeleton className="mt-2 h-4 w-full max-w-lg" />
          </div>
        ))}
      </div>
    </div>
  );
}
