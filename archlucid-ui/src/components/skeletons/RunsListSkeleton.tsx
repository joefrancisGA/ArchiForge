import { Skeleton } from "@/components/ui/skeleton";

/**
 * Mirrors the runs list page: heading, controls row, and table-shaped placeholders.
 */
export function RunsListSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading runs list" className="space-y-4">
      <Skeleton className="h-7 w-48" />
      <Skeleton className="h-4 w-full max-w-xl" />
      <div className="flex flex-col gap-3 sm:flex-row sm:flex-wrap sm:items-end">
        <Skeleton className="h-10 w-full max-w-md" />
        <Skeleton className="h-10 w-40" />
        <Skeleton className="h-4 w-56" />
      </div>
      <div className="overflow-x-auto rounded-md border border-neutral-200 dark:border-neutral-700">
        <table className="w-full border-collapse text-sm">
          <thead>
            <tr className="border-b border-neutral-200 dark:border-neutral-700">
              <th className="p-2 text-left">
                <Skeleton className="h-4 w-16" />
              </th>
              <th className="p-2 text-left">
                <Skeleton className="h-4 w-14" />
              </th>
              <th className="p-2 text-left">
                <Skeleton className="h-4 w-24" />
              </th>
              <th className="p-2 text-left">
                <Skeleton className="h-4 w-16" />
              </th>
              <th className="p-2 text-left">
                <Skeleton className="h-4 w-20" />
              </th>
            </tr>
          </thead>
          <tbody>
            {[1, 2, 3, 4, 5].map((row) => (
              <tr key={row} className="border-b border-neutral-100 dark:border-neutral-800">
                <td className="p-2">
                  <Skeleton className="h-4 w-28" />
                </td>
                <td className="p-2">
                  <Skeleton className="h-4 w-16" />
                </td>
                <td className="p-2">
                  <Skeleton className="h-4 w-48" />
                </td>
                <td className="p-2">
                  <Skeleton className="h-4 w-24" />
                </td>
                <td className="p-2">
                  <Skeleton className="h-4 w-20" />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
      <div className="flex flex-wrap gap-4">
        <Skeleton className="h-4 w-40" />
        <Skeleton className="h-4 w-20" />
        <Skeleton className="h-4 w-16" />
      </div>
    </div>
  );
}
