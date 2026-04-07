import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AlertsLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading alerts.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching alerts from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
