import { OperatorBrandedNotFound } from "@/components/OperatorBrandedNotFound";

/** Operator-shelled Not Found (invalid deep links, stale ids, malformed route tokens). */
export default function OperatorNotFound() {
  return (
    <main className="mx-auto max-w-4xl px-1 py-6 sm:px-0">
      <OperatorBrandedNotFound />
    </main>
  );
}
