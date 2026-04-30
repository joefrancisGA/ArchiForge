import { OperatorBrandedNotFound } from "@/components/OperatorBrandedNotFound";

/**
 * Global 404 when no route matches (outside a segment-specific `not-found`). Minimal chrome — no operator shell.
 */
export default function GlobalNotFound() {
  return (
    <main className="mx-auto min-h-[50vh] max-w-4xl px-4 py-12">
      <OperatorBrandedNotFound />
    </main>
  );
}
