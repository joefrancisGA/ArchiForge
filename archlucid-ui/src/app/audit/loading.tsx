import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AuditLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading audit log.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing filters and event types…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
