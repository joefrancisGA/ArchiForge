import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function GovernanceResolutionLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading governance resolution.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching effective governance from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
