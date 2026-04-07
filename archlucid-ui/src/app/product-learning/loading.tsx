import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function ProductLearningLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading pilot feedback.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Opening the product learning dashboard…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
