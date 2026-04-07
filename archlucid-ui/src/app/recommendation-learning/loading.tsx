import { OperatorLoadingNotice } from "@/components/OperatorShellMessage";

export default function RecommendationLearningLoading() {
  return (
    <main>
      <OperatorLoadingNotice>
        <strong>Loading recommendation learning.</strong>
        <p style={{ margin: "8px 0 0", fontSize: 14 }}>Fetching learning profile from the API…</p>
      </OperatorLoadingNotice>
    </main>
  );
}
