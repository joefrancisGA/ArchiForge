import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AlertSimulationLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading alert simulation.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Preparing simulation tools…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
