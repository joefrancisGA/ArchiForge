import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function ManifestLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading manifest.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching manifest summary and artifacts…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
