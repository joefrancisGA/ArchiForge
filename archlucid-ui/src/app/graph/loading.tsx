import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function GraphLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading graph viewer.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>
          Opening the graph tools page and wiring client-side controls…
        </p>
      </OperatorLoadingNotice>
    </main>
  );
}
