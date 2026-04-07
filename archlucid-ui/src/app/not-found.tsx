import Link from "next/link";

import { OperatorEmptyState } from "@/components/OperatorShellMessage";

/**
 * Custom 404 for unknown routes. Keeps operator shell chrome (root layout) and avoids
 * default Next.js framing that can leak framework identity (WAF SE:01).
 */
export default function NotFound() {
  return (
    <main>
      <OperatorEmptyState title="Page not found">
        <p style={{ margin: 0, fontSize: 14 }}>
          That URL does not match any operator view. Check the path or use the navigation above.
        </p>
        <div style={{ marginTop: 16, display: "flex", gap: 16, flexWrap: "wrap" }}>
          <Link href="/" style={{ fontSize: 14 }}>
            Home
          </Link>
          <Link href="/runs" style={{ fontSize: 14 }}>
            Runs
          </Link>
          <Link href="/compare" style={{ fontSize: 14 }}>
            Compare
          </Link>
        </div>
      </OperatorEmptyState>
    </main>
  );
}
