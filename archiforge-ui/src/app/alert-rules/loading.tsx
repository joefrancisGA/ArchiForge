import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AlertRulesLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading alert rules.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching rules from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
