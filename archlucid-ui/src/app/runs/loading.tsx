import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function RunsLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading runs.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>
          Fetching the run list from the API for the project in the URL (default <code>default</code>). Large
          tenants may take a few seconds.
        </p>
      </OperatorLoadingNotice>
    </main>
  );
}
