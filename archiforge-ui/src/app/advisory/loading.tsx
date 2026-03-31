import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AdvisoryLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading advisory.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching advisory scan data from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
