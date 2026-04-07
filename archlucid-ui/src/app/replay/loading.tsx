import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function ReplayLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading replay.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Opening the replay tools…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
