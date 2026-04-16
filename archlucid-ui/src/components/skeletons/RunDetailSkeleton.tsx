import { Skeleton } from "@/components/ui/skeleton";

/**
 * Mirrors run detail: title, metadata grid, progress strip, section tabs, and two content panels.
 */
export function RunDetailSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading run detail" className="space-y-6">
      <Skeleton className="h-7 w-56" />
      <div className="grid max-w-3xl grid-cols-1 gap-3 sm:grid-cols-2">
        {[1, 2, 3, 4, 5, 6].map((i) => (
          <div key={i} className="space-y-1">
            <Skeleton className="h-3 w-24" />
            <Skeleton className="h-4 w-full" />
          </div>
        ))}
      </div>
      <Skeleton className="h-3 w-full max-w-2xl" />
      <div className="flex flex-wrap gap-2">
        {[1, 2, 3, 4].map((i) => (
          <Skeleton key={i} className="h-8 w-24" />
        ))}
      </div>
      <div className="grid gap-4 lg:grid-cols-2">
        <div className="space-y-3 rounded-lg border border-neutral-200 p-4 dark:border-neutral-700">
          <Skeleton className="h-5 w-40" />
          <Skeleton className="h-32 w-full" />
        </div>
        <div className="space-y-3 rounded-lg border border-neutral-200 p-4 dark:border-neutral-700">
          <Skeleton className="h-5 w-36" />
          <Skeleton className="h-32 w-full" />
        </div>
      </div>
    </div>
  );
}
