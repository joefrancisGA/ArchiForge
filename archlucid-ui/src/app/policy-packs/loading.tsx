import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function PolicyPacksLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading policy packs.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching packs from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
