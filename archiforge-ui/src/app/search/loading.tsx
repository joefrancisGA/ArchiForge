import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function SearchLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading search.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing retrieval search…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
