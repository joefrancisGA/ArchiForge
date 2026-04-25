"use client";

import { useCallback, useEffect, useState } from "react";

import { ContextualHelp } from "@/components/ContextualHelp";
import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card";
import { mergeRegistrationScopeForProxy } from "@/lib/proxy-fetch-registration-scope";

const USERS_PATH = "/api/proxy/v1/admin/users";

type UserRow = {
  userId: string;
  displayName: string;
  email: string;
  authorityLabel: string;
};

/**
 * Tenant user directory and rank assignment. Editing requires API routes that are not yet wired in this repo;
 * the page stays read-only until GET/PUT admin user endpoints are available.
 */
export default function AdminUsersPage() {
  const [loading, setLoading] = useState(true);
  const [rows, setRows] = useState<UserRow[]>([]);
  const [note, setNote] = useState<string | null>(null);

  const load = useCallback(async () => {
    setLoading(true);
    setNote(null);
    try {
      const res = await fetch(USERS_PATH, mergeRegistrationScopeForProxy({ headers: { Accept: "application/json" } }));
      if (!res.ok) {
        setRows([]);
        setNote(
          "User management API endpoints are required to enable editing. Expected GET /v1/admin/users (or equivalent) is not available yet.",
        );
        return;
      }
      const json: unknown = await res.json();
      const parsed = parseUsersPayload(json);
      if (parsed.length === 0) {
        setRows([]);
        setNote("The user list response was empty.");
        return;
      }
      setRows(parsed);
    } catch {
      setRows([]);
      setNote("Could not load users for this tenant.");
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    void load();
  }, [load]);

  return (
    <main className="mx-auto max-w-4xl space-y-6" data-testid="admin-users-page">
      <div>
        <div className="flex items-start gap-2">
          <h1 className="text-xl font-semibold text-neutral-900 dark:text-neutral-100">Users & roles</h1>
          <ContextualHelp helpKey="admin-users-page" />
        </div>
        <p className="mt-1 text-sm text-neutral-600 dark:text-neutral-400">Directory of principals in this tenant and their ArchLucid authority tier (Reader / Operator / Admin).</p>
      </div>

      <Card>
        <CardHeader>
          <CardTitle className="text-base">Users</CardTitle>
        </CardHeader>
        <CardContent>
          {loading ? <p className="m-0 text-sm text-neutral-500">Loading…</p> : null}
          {!loading && note !== null ? (
            <p className="m-0 text-sm text-amber-900 dark:text-amber-100" data-testid="admin-users-api-note">
              {note}
            </p>
          ) : null}
          {!loading && rows.length > 0 ? (
            <div className="overflow-x-auto">
              <table className="w-full text-left text-sm">
                <thead>
                  <tr className="border-b border-neutral-200 text-xs uppercase text-neutral-500 dark:border-neutral-700">
                    <th className="py-2 pr-3">Display name</th>
                    <th className="py-2 pr-3">Email</th>
                    <th className="py-2 pr-3">Authority</th>
                  </tr>
                </thead>
                <tbody>
                  {rows.map((r) => {
                    return (
                      <tr key={r.userId} className="border-b border-neutral-100 dark:border-neutral-800">
                        <td className="py-2 pr-3 font-medium text-neutral-900 dark:text-neutral-100">{r.displayName}</td>
                        <td className="py-2 pr-3 text-neutral-600 dark:text-neutral-300">{r.email}</td>
                        <td className="py-2 pr-3 text-neutral-600 dark:text-neutral-300">{r.authorityLabel}</td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          ) : null}
        </CardContent>
      </Card>
    </main>
  );
}

function parseUsersPayload(json: unknown): UserRow[] {
  if (json === null || typeof json !== "object") {
    return [];
  }
  const root = json as { users?: unknown; items?: unknown };
  const raw = Array.isArray(root.users) ? root.users : Array.isArray(root.items) ? root.items : null;
  if (raw === null) {
    return [];
  }
  const out: UserRow[] = [];
  for (const u of raw) {
    if (u === null || typeof u !== "object") {
      continue;
    }
    const o = u as Record<string, unknown>;
    const userId = String(o.userId ?? o.id ?? "");
    if (userId.length === 0) {
      continue;
    }
    const displayName = String(o.displayName ?? o.name ?? "—");
    const email = String(o.email ?? "—");
    const rank = o.authorityRank;
    const role = o.role ?? o.maxAuthority;
    const authorityLabel =
      typeof role === "string" && role.length > 0
        ? role
        : typeof rank === "number" && Number.isFinite(rank)
          ? `Rank ${rank}`
          : "—";
    out.push({ userId, displayName, email, authorityLabel });
  }
  return out;
}
