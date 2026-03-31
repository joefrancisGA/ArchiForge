import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function AlertRoutingLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading alert routing.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching routing rules from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
