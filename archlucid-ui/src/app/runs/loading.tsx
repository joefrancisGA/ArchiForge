import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function RunsLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading runs.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching the run list from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
