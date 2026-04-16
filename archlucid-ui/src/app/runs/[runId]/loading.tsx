import { RunDetailSkeleton } from "@/components/skeletons/RunDetailSkeleton";

/** Next.js loading UI while the server run detail page fetches run, manifest, artifacts, and explanation. */
export default function RunDetailLoading() {
  return (
    <main className="mx-auto max-w-4xl px-1 py-4 sm:px-0">
      <RunDetailSkeleton />
    </main>
  );
}
