import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AlertTuningLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading alert tuning.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching tuning data from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
