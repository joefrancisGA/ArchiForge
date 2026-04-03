import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function PlanningLoading() {
  return (
    <main style={{ maxWidth: 960 }}>
      <OperatorLoadingNotice>
        <strong>Loading planning data.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching themes and improvement plans…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
