> **Scope:** Top 10 operator-visible failure modes (`56R`-style quick fixes) anchored to shipped configuration—not exhaustive root-cause analysis.

# Common operator errors — top 10

**Audience:** pilots + on-call responders. Prefer **[TROUBLESHOOTING.md](../TROUBLESHOOTING.md)** first-pass flow; this doc expands repeatable failures.

---

## 1. API exits at startup — **SQL connection string missing / unreachable**

**Symptom:** Log shows DbUp/connectivity failure; **`ConnectionStrings:ArchLucid`** error.

**Cause:** **`ArchLucid:StorageProvider`** is **`Sql`** but the connection string cannot open SQL (`localhost` firewall, credential, typo).

**Resolution:** Align **`ConnectionStrings:ArchLucid`** with your SQL reachable host; Docker compose users: ensure MSSQL healthy first. **`dotnet user-secrets`** or Key Vault-backed settings in staging/prod (**[CONFIGURATION_KEY_VAULT.md](../library/CONFIGURATION_KEY_VAULT.md)**).

**Prevention:** Put secrets only in vault / secret stores—avoid committing rotated passwords.

---

## 2. **`DevelopmentBypass` refused in staging/prod-shaped hosts**

**Symptom:** `InvalidOperationException` referencing **`DevelopmentBypass`** not allowed (`AuthSafetyGuard`).

**Cause:** **`ArchLucidAuth:Mode=DevelopmentBypass`** configured while **`ASPNETCORE_ENVIRONMENT`** implies non-Development (**Staging**/**Production**/etc.).

**Resolution:** Flip to **`ApiKey`** or **`JwtBearer`** with working identity material; unset bypass flags.

**Prevention:** Keep compose **dev stacks** labelled Development; staging/prod manifests should never carry bypass switches.

See **[SECURITY.md](../library/SECURITY.md)** § DevelopmentBypass production guard.

---

## 3. **401 Unauthorized** everywhere

**Symptom:** Swagger/CLI/UI receive **401** with **WWW-Authenticate** challenges.

**Cause:** **`Authentication:ApiKey:Enabled=false`** (fail-closed) or missing keys / wrong header.

**Resolution:** Populate **`Authentication:ApiKey:AdminKey`/`ReadOnlyKey`** (environment variables) and set **`Enabled=true`**; or supply valid Bearer token per **[API_CONTRACTS.md](../library/API_CONTRACTS.md)**.

**Prevention:** Document API key rollout in sprint handoff wiki.

---

## 4. **DbUp / migration failures** on boot

**Symptom:** Stack trace under **DbUp** / migration number; readiness failure.

**Cause:** Older schema objects, conflicting manual DDL, insufficient DB privileges, paused Azure SQL tier.

**Resolution:** Inspect **first failing script** lines; restore DB snapshot if needed; run against disposable DB to reproduce; escalate with **`DatabaseMigration`** log correlation id.

**Prevention:** Never hand-edit **`ArchLucid.sql`** outside approved migration sequencing rules.

See **[SQL_SCRIPTS.md](../library/SQL_SCRIPTS.md)**.

---

## 5. **Real-mode agent** timeouts / breaker open — **missing Azure OpenAI**

**Symptom:** Alerts citing **`AzureOpenAI`**, breaker **Open**, or agent execution timeouts.

**Cause:** **`AgentExecution:Mode=Real`** requires endpoint + key/model deployment reachable.

**Resolution:** Prefer **Simulator** for dry runs; configure **`AzureOpenAI`** section (`Endpoint`, **`ApiKey`/managed identity**) or fix network egress / private endpoints.

**Prevention:** Maintain **[RESILIENCE_CONFIGURATION.md](../library/RESILIENCE_CONFIGURATION.md)** non-default tuned profile per environment.

---

## 6. **ContentSafety** enforced but **misconfigured SDK**

**Symptom:** Startup validation errors for **`ArchLucid:ContentSafety:Endpoint`/`ApiKey`** (production-like hosts).

**Cause:** Turning on **`ArchLucid:ContentSafety:Enabled`** without pairing Azure AI Content Safety resources.

**Resolution:** Provision Content Safety endpoint + key **or** run under dev profile with guard disabled per **[SECURITY.md](../library/SECURITY.md)** matrix.

---

## 7. **403 / empty scopes** despite good auth — tenant / RLS mismatch

**Symptom:** Reads return empty sets or **`403 Forbidden`**.

**Cause:** JWT claims omit tenant/workspace/project; **`SESSION_CONTEXT`** not propagated; stray scope headers.

**Resolution:** Align **scope headers**/claims with seeded tenant GUIDs (`TenantId`/`WorkspaceId`/`ProjectId`); inspect **`IScopeContextProvider`** debug logs (**Debug** posture only).

**Prevention:** Follow **[CUSTOMER_TRUST_AND_ACCESS.md](../library/CUSTOMER_TRUST_AND_ACCESS.md)** onboarding scripts.

See **[MULTI_TENANT_RLS.md](../security/MULTI_TENANT_RLS.md)**.

---

## 8. **429 Too Many Requests**

**Symptom:** HTTP **429**, rate-limit problem details extensions.

**Cause:** Burst traffic crosses **`RateLimiting:FixedWindow`** / **`Expensive`** budget for partition (role/IP).

**Resolution:** Respect **`Retry-After`**, backoff automations; increase budgets **temporarily only** in staging for load tests—not prod without capacity review (**[LOAD_TEST_BASELINE.md](../library/LOAD_TEST_BASELINE.md)**).

---

## 9. **`409 Conflict` on manifest commit**

**Symptom:** Commit endpoint returns concurrency / state conflict (**`ROWVERSION`**, stale ETag equivalents).

**Cause:** Concurrent writers or outdated client view of **`Run`** state.

**Resolution:** Reload latest run aggregate; reconcile tasks; escalate if repeatable under single writer (capture **`correlationId`**).

**Prevention:** UI clients adopt optimistic concurrency headers per **[API_CONTRACTS.md](../library/API_CONTRACTS.md)** mutate guidance.

---

## 10. **`/health/ready`** unhealthy despite `/health/live` OK

**Symptom:** Liveness succeeds; readiness surfaces failing dependency (**SQL** / **Redis** / **rule pack** / **disk**).

**Cause:** Mapped readiness checks include each critical dependency—which may be degraded independently.

**Resolution:** Inspect JSON **`entries[]`** ordering = priority; remedy each failing **`description`** (**SQL** reachable? **Redis** TLS? Blob permissions? disk space?).

**Prevention:** Synthetic monitors and the scheduled **`hosted-saas-probe`** workflow (`.github/workflows/hosted-saas-probe.yml`) for production SaaS (**[SLA_TARGETS.md](../library/SLA_TARGETS.md)**).


---

### Tools

```powershell
dotnet run --project .\ArchLucid.Cli -- doctor --url https://localhost:5001
dotnet run --project .\ArchLucid.Cli -- support-bundle --out .\diag.zip --url https://localhost:5001
```

Record **`GET /version`** output with support tickets (**[CLI_USAGE.md](../library/CLI_USAGE.md)**).
