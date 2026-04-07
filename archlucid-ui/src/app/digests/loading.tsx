import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function DigestsLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading digests.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching digests from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
