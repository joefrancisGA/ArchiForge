import { GenericPageSkeleton } from "@/components/skeletons/GenericPageSkeleton";

/** Loading placeholder for coordinator provenance (graph + timeline). */
export default function RunProvenanceLoading() {
  return (
    <main className="mx-auto max-w-4xl px-1 py-4 sm:px-0">
      <GenericPageSkeleton />
    </main>
  );
}
