import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AskLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading ask.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing the ask workspace…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
