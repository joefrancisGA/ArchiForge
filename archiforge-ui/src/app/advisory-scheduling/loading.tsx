import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AdvisorySchedulingLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading advisory scheduling.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching schedules from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
