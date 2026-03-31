import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function DigestSubscriptionsLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading digest subscriptions.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching subscriptions from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
