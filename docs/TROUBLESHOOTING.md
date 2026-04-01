# Troubleshooting for pilots and operators (56R)

**Goal:** Faster triage without reading the whole codebase.

## First-line steps (try in order)

1. **`GET /health/live`** — process up? Then **`GET /health/ready`** — read JSON `entries[]` for the first **`Unhealthy`** / **`Degraded`** check.
2. **`GET /version`** — capture build identity for your report (same info appears in enriched **`/health/ready`** / **`/health`** JSON as **`version`** / **`commitSha`**).
3. **`dotnet run --project ArchiForge.Cli -- doctor`** (repo root, API reachable) — CLI + API version + all health endpoints.
4. **`support-bundle --zip`** — sanitized diagnostics folder (review before sending). See [Support bundle](#support-bundle-attach-to-tickets) below.
5. **`run-readiness-check`** or **`release-smoke -SkipE2E`** — confirm your clone builds and fast core tests pass ([RELEASE_LOCAL.md](RELEASE_LOCAL.md), [RELEASE_SMOKE.md](RELEASE_SMOKE.md)).

If still stuck, use **[When you report an issue](PILOT_GUIDE.md#when-you-report-an-issue)** in [PILOT_GUIDE.md](PILOT_GUIDE.md).

---

## Problem Details (`application/problem+json`) and `supportHint`

API error responses may include:

- **`detail`** — what went wrong (safe for operators; not stack traces).
- **`extensions.errorCode`** — stable machine-readable code (e.g. `RUN_NOT_FOUND`).
- **`extensions.supportHint`** — short **next step** for common situations (no secrets).

The **ArchiForge CLI** prints **`Next:`** lines on **stderr** after many failures, aligned with the same guidance.

**Operator UI:** JSON error bodies from **`/api/proxy/*`** may include **`supportHint`** when the proxy cannot reach the API (502) or when upstream URL configuration is invalid (503).

---

## Quick matrix

| Symptom | Likely cause | What to try |
|--------|----------------|-------------|
| API **does not start**; log mentions migration / DbUp | Bad **connection string**, DB unreachable, or migration failure | Fix `ConnectionStrings:ArchiForge`. Confirm SQL is up. See log lines mentioning **DbUp** or **migration**. [BUILD.md](BUILD.md), [SQL_SCRIPTS.md](SQL_SCRIPTS.md) |
| **`/health/ready`** returns **503** | Database (when using Sql), schema files, rule pack, or temp directory check failed | Read JSON body for which check failed. Fix config/paths/permissions. |
| **`401` / `403`** on API | Auth mode / role mismatch | **Development:** ensure `ArchiForgeAuth` is **DevelopmentBypass** for local pilots. **JWT:** confirm token roles map to Reader/Operator/Admin. [README.md](../README.md#api-authentication-archiforgeauth) |
| **`429 Too Many Requests`** | Rate limiting | Wait for the window to reset or adjust `RateLimiting:*` in config (non-production). |
| **`404`** on run or manifest | Wrong **run ID**, wrong **scope** (tenant/workspace/project), or data not in that scope | Re-use default scope headers or match the scope used at create time. |
| **`409`** on commit | Run state / idempotency conflict | Follow message; may need to re-fetch run status or use a fresh run. [API_CONTRACTS.md](API_CONTRACTS.md) |
| UI shows **503** JSON “Invalid upstream API configuration” | **`ARCHIFORGE_API_BASE_URL`** missing or invalid in **`.env.local`** | Set server-side base URL in `archiforge-ui/.env.local`. Restart `npm run dev`. |
| UI loads but API calls fail | Proxy or CORS | Check **browser network** tab and **Next server logs** (look for **`archiforge-ui-proxy`** JSON warnings). Confirm API URL and that API allows your UI origin under **`Cors:AllowedOrigins`**. |
| **`run --quick` / execute** fails with LLM or timeout errors | **Real agent** mode without valid Azure OpenAI config | For pilots, prefer **simulator** / default dev settings so no cloud keys are required. Check `AgentExecution` / related appsettings. |
| .NET tests fail with SQL errors | No SQL Server for integration tests | Set **`ARCHIFORGE_SQL_TEST`** or **`ARCHIFORGE_API_TEST_SQL`** (Linux/macOS/CI), or run **fast core** only. [BUILD.md](BUILD.md) |

---

## API startup failures

1. Read the **console output** from first line to first `InvalidOperationException` / stack stop.
2. **Configuration validation** runs **right after** the host is built: errors are logged as **`Startup configuration error:`** — fix each listed setting.
3. If **`ConnectionStrings:ArchiForge`** is unset while **`ArchiForge:StorageProvider`** is **`Sql`**, startup will fail once DB is required.

---

## Logs — what to search for

- **`RunId=`** — ties log lines to a single architecture run.
- **`X-Correlation-ID`** you sent on the request (or the ID the server returned) — ties client attempts to server handling.
- **`Authority pipeline`** / **`Architecture run execution failed`** — authority vs application run paths.
- **`archiforge-ui-proxy`** — UI server-side forwarder problems (upstream status, bad base URL).

Logs go to **stdout** unless your host redirects them (Docker/Kubernetes, IIS, Windows Service).

---

## Artifact list empty or download 404

- An **empty artifact list** (`[]`) can be valid: manifest exists but **no synthesized files** yet or **none stored** for that manifest.
- **Bundle ZIP 404** can mean “no bundle” vs “manifest not found” depending on API **ProblemDetails** — compare `title` / `type` / `detail` in the response.

See [operator-shell.md](operator-shell.md) and [API_CONTRACTS.md](API_CONTRACTS.md).

---

## Support bundle (attach to tickets)

With the **API running**, from repo root (or set **`ARCHIFORGE_API_URL`** to your API base):

```bash
dotnet run --project ArchiForge.Cli -- support-bundle --zip
```

Default output: folder **`support-bundle-<yyyyMMdd-HHmmss>Z`** in the current directory, plus a **`.zip`** of the same files when **`--zip`** is set.

```bash
dotnet run --project ArchiForge.Cli -- support-bundle --output ./my-bundle --zip
```

**Contents (JSON only):** `manifest.json`, `build.json` (CLI build + raw **`GET /version`**), `health.json` (`/health/live`, `/health/ready`, `/health`, truncated bodies), `config-summary.json`, `environment.json` (filtered), `workspace.json`, `references.json`, `logs.json`. Secrets are not copied literally: sensitive env names are **`(set)`** / **`(not set)`**; **`ARCHIFORGE_*`** keys containing **`SQL`** never expose values; HTTP URLs may be redacted. **Review** before sending externally.

Full CLI flags: [CLI_USAGE.md](CLI_USAGE.md).

---

## Still stuck?

1. Run **`dotnet run --project ArchiForge.Cli -- doctor`** with the API up.
2. Run **`support-bundle --zip`** (above) and attach the archive after redacting anything your policy still forbids.
3. Run **`run-readiness-check.cmd`** (or `.ps1`) to confirm build + fast core + UI unit tests on your machine.
4. For an automated **API + CLI + artifact** check, see **[RELEASE_SMOKE.md](RELEASE_SMOKE.md)** (`release-smoke` — requires SQL for the E2E block unless `-SkipE2E`).
5. Open **[PILOT_GUIDE.md](PILOT_GUIDE.md)** — first-run narrative and **[what to send when reporting an issue](PILOT_GUIDE.md#when-you-report-an-issue)**.
