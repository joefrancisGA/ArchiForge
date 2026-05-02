"use client";

import { useEffect, useState } from "react";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { OperatorApiProblem } from "@/components/OperatorApiProblem";
import { toApiLoadFailure } from "@/lib/api-load-failure";
import type { ApiLoadFailureState } from "@/lib/api-load-failure";
import { getAuthHeaders } from "@/lib/api-auth";

interface RoiTelemetry {
  totalRuns: number;
  totalHoursSaved: number;
  averageTimeToCommitMs: number;
}

export function ValueRealizationDashboard() {
  const [telemetry, setTelemetry] = useState<RoiTelemetry | null>(null);
  const [loading, setLoading] = useState(true);
  const [failure, setFailure] = useState<ApiLoadFailureState | null>(null);

  useEffect(() => {
    async function load() {
      try {
        const response = await fetch("/api/proxy/v1/architecture/telemetry/roi", {
          headers: getAuthHeaders(),
        });
        if (!response.ok) {
          throw new Error(`Failed to load telemetry: ${response.statusText}`);
        }
        const data = await response.json();
        setTelemetry(data);
      } catch (e) {
        setFailure(toApiLoadFailure(e));
      } finally {
        setLoading(false);
      }
    }
    void load();
  }, []);

  if (loading) {
    return <div className="text-sm text-neutral-500">Loading Value Realization metrics...</div>;
  }

  if (failure) {
    return (
      <OperatorApiProblem
        problem={failure.problem}
        fallbackMessage={failure.message}
        correlationId={failure.correlationId}
      />
    );
  }

  if (!telemetry) {
    return null;
  }

  return (
    <Card className="mb-6">
      <CardHeader>
        <CardTitle className="text-lg">Value Realization</CardTitle>
      </CardHeader>
      <CardContent>
        <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Total Runs</p>
            <p className="text-2xl font-bold">{telemetry.totalRuns}</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Hours Saved</p>
            <p className="text-2xl font-bold text-teal-600">{telemetry.totalHoursSaved}</p>
          </div>
          <div className="rounded-lg border p-4 text-center">
            <p className="text-sm text-neutral-500">Avg Time to Commit</p>
            <p className="text-2xl font-bold">
              {Math.round(telemetry.averageTimeToCommitMs / 60000)} mins
            </p>
          </div>
        </div>
      </CardContent>
    </Card>
  );
}
