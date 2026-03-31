import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function CompositeAlertRulesLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading composite alert rules.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching composite rules from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
