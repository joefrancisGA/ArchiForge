import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function RunDetailLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading run detail.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>
          Loading run, manifest summary, and artifact list where applicable…
        </p>
      </OperatorLoadingNotice>
    </main>
  );
}
