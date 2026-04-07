import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function Loading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing the operator shell view…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
