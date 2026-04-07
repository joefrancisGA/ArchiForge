import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function CompareLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading compare.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Opening the run comparison tools…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
