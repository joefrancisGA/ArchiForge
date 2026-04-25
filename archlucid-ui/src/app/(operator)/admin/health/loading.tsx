import { Card, CardContent, CardHeader } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/skeleton";

/**
 * Route-level shell while system health data is loading.
 */
export default function AdminHealthLoading() {
  return (
    <div className="mx-auto max-w-3xl space-y-6" aria-busy>
      <div className="space-y-2">
        <Skeleton className="h-8 w-64" />
        <Skeleton className="h-4 w-full max-w-lg" />
        <Skeleton className="h-9 w-24" />
      </div>
      {["a", "b", "c"].map((k) => {
        return (
          <Card key={k}>
            <CardHeader>
              <Skeleton className="h-5 w-48" />
              <Skeleton className="h-3 w-full max-w-xl" />
            </CardHeader>
            <CardContent>
              <Skeleton className="h-32 w-full" />
            </CardContent>
          </Card>
        );
      })}
    </div>
  );
}
