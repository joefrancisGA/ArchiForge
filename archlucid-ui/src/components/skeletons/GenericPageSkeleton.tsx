import { Skeleton } from "@/components/ui/skeleton";

/**
 * Generic route loading placeholder when a page-specific skeleton is not defined.
 */
export function GenericPageSkeleton() {
  return (
    <div aria-busy="true" aria-label="Loading page content" className="space-y-4">
      <Skeleton className="h-7 w-48" />
      <Skeleton className="h-4 w-full max-w-xl" />
      <Skeleton className="h-4 w-full max-w-lg" />
      <Skeleton className="h-48 w-full max-w-4xl rounded-lg" />
    </div>
  );
}
