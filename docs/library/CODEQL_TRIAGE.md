> **Scope:** CodeQL triage (ArchLucid) - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# CodeQL triage (ArchLucid)

Short guide for **security-and-quality** (or **javascript-typescript-security-and-quality**) workflows. Use this when alerts look like noise after mitigations are in place.

---

## Log entries created from user input (CWE-117)

### True positives

Treat as a **real issue** when an **`ILogger`** call logs a **`string`-typed** parameter that comes from untrusted HTTP surface area, for example:

- **`[FromBody]`** DTO string properties  
- **`[FromQuery]`** / **`[FromHeader]`** string parameters  
- **`HttpContext.Request.Path`** (or **`.Path.Value`**) and similar raw path strings  

**Fix:** pass the value through **`LogSanitizer.Sanitize()`** from **`ArchLucid.Core.Diagnostics`** before logging. See **`docs/SECURITY.md`** (Log injection / CWE-117).

### CodeQL model pack (`cs/log-forging` and `LogSanitizer`)

Built-in **`cs/log-forging`** does not know your custom sanitizer unless you model it. This repo ships a **CodeQL model pack** that registers **`LogSanitizer.Sanitize`** as a **`log-injection`** barrier (aligned with **`LogForgingQuery`**’s **`barrierNode(..., "log-injection")`** in upstream CodeQL).

- **Pack:** `.github/codeql/archlucid-csharp-log-sanitizer-models/` (`qlpack.yml` + `models/*.yml`)
- **Workflow wiring:** `.github/codeql/codeql-config.yml` lists that pack under **`packs.csharp`**; **`.github/workflows/codeql.yml`** passes **`config-file`** only on the **csharp** job’s **`init`** step (the JavaScript job is unchanged).

After this is merged, **`LogSanitizer.Sanitize(...)`** call sites should stop alerting as unsanitized user input. If an alert remains, check the sink is actually the sanitizer’s **return value** (not a raw parameter) and that the method signature still matches **`(System.String)`** in the model file.

**Copilot Autofix** for CodeQL cannot infer custom sanitizers; use this model pack (or dismiss manually with rationale).

### `LoggerExtensions.LogWarning(ILogger, Exception?, string?, params object?[])` (boxing)

Some **`cs/log-forging`** findings persist even when the template argument is **`LogSanitizer.Sanitize(...)`** at the call site: the sanitizer’s return value is boxed into **`params object?[]`**, and the query may not treat the custom barrier as effective across that hop. The same applies to **`LogInformation`** and other **`LoggerExtensions`** methods that take **`params object?[]`**.

**Mitigation in this repo:** use **`ArchLucid.Core.Diagnostics.SanitizedLoggerWarningExtensions.LogWarningWithSanitizedUserArg`**, which sanitizes immediately before the **`LogWarning`** call. A **`// codeql[cs/log-forging]`** suppression on the sink (inside Core) documents the remaining false positive for exception context + **`params object?[]`** at the helper boundary.

For **multi-placeholder `LogInformation`** in **`ArchLucid.Application`**, **`ArchLucid.AgentRuntime`**, or **`ArchLucid.Host.Core`** (no reference to **`ArchLucid.Api`**), use **`ArchLucid.Core.Diagnostics.SanitizedLoggerInformationExtensions`** (**`LogInformationArchitectureRunCommitted`**, **`LogInformationCommitRunIdempotentReturn`**, **`LogInformationGovernanceManifestPromoted`**, **`LogInformationGovernanceEnvironmentActivated`**, **`LogInformationComparisonReplaySucceeded`**, **`LogInformationAgentExecutionBatchStarting`**, **`LogInformationAgentExecutionBatchCompleted`**, **`LogInformationAgentResultSubmitted`** (coordinator **`ArchitectureApplicationService`** agent-result success log), **`LogInformationCreatingArchitectureRun`**, or add a sibling method there) so sanitization sits adjacent to the sink inside Core. **All Information helpers in this class now delegate to a private `[LoggerMessage]`-generated emitter** (declared in **`SanitizedLoggerInformationExtensions.LoggerMessage.cs`**, EventId range **3001–3009**); the source generator emits cached, strongly-typed `Action<ILogger, T1, …>` delegates (or — for the 8-parameter `EmitComparisonReplaySucceeded` — a typed `__EmitComparisonReplaySucceededStruct` `IReadOnlyList<KeyValuePair<string, object?>>` state). That removes the **`params object?[]`** boxing entirely, so the **`LogSanitizer.Sanitize`** barrier registered in **`.github/codeql/archlucid-csharp-log-sanitizer-models`** propagates straight to the **`ILogger.Log<TState>`** sink and **no `// codeql[cs/log-forging]` annotation is required on these helpers**. Adding new sibling helpers should follow the same pattern (public sanitizing wrapper → private `[LoggerMessage]` partial emitter; do **not** call `LogInformation(template, params object?[])` directly). For **`LogDebug`** with the same boxing issue, use **`SanitizedLoggerDebugExtensions`** (e.g. **`LogDebugAgentTaskFinished`**) — until that class is migrated to the same `[LoggerMessage]` pattern, the existing trailing **`// codeql[cs/log-forging]`** annotation guidance below still applies. For SQL **host leader election** telemetry (lease name + instance id), use **`SanitizedLoggerHostLeaderElectionExtensions`** so **`cs/exposure-of-sensitive-information`** and **`cs/log-forging`** noise stays centralized. For **two string placeholders at `LogWarning`**, use **`SanitizedLoggerWarningExtensions.LogWarningWithTwoSanitizedUserStrings`**; for **three** (e.g. coordination validation: request id, system name, joined errors), use **`LogWarningWithThreeSanitizedUserStrings`** (same wrapper pattern). For **comparison replay failure** (`Exception` + record id + message + boolean flags), use **`SanitizedLoggerWarningExtensions.LogWarningComparisonReplayFailed`** so sanitized strings sit next to the sink (avoids **`cs/log-forging`** false positives on **`params object?[]`** boxing and on boolean placeholders). For **`LogError`** with **`Exception`** + HTTP method and path (e.g. **`WorkerHostPipelineExtensions`** unhandled exception handler), use **`SanitizedLoggerErrorExtensions.LogErrorUnhandledWorkerHttpRequest`**. If CodeQL still alerts after **`LogSanitizer.Sanitize`** on a direct **`LogInformation`** / **`LogWarning`** call that has not yet been migrated to **`[LoggerMessage]`**, add **`// codeql[cs/log-forging]`** on the **same line as the sink** (for a multi-line **`LogInformation`** / **`LogWarning`** call, typically the line with the closing **`);`**). A comment only above the opening **`_logger.Log…(`** line is often **not** picked up. The preferred remediation, however, is to migrate the helper to the `[LoggerMessage]` pattern as **`SanitizedLoggerInformationExtensions`** does, which removes the boxing entirely and eliminates the need for an inline suppression.

**Concrete:** both idempotent commit return paths in **`ArchitectureRunCommitOrchestrator`** (**`TryReturnCommittedManifestAsync`** and **`TryReturnPersistedCommitIfExistsAsync`**) must call **`LogInformationCommitRunIdempotentReturn`** — not **`_logger.LogInformation(..., LogSanitizer.Sanitize(runId), …)`** — or **`cs/log-forging`** will likely return (params boxing breaks the custom sanitizer model).

### False positives

Treat as a **false positive** when the logged parameter is a **value type** bound from **`[FromRoute]`** (or otherwise not arbitrary attacker-controlled string content), e.g. **`Guid`**, **`int`**, **`DateTime`**. Their formatted output does not carry the same newline/control-character injection risk as arbitrary strings.

**Dismiss in the GitHub CodeQL / code scanning UI** with a reason along the lines of:

> False positive — value type cannot contain control characters.

(Adjust the note if the query specifically references `ToString()` on a value type.)

### Known alerts to triage (run / approval identifiers)

**Value-type binding (typical false positive for CWE-117):**

| Location | What is logged | Parameter type in code |
| -------- | -------------- | ------------------------ |
| **`ArchLucid.Api/Controllers/Planning/ExplanationController.cs`** | **`runId`** in provenance / explanation warnings | **`Guid`** with route template **`{runId:guid}`** |

Dismiss with: *False positive — value type cannot contain control characters* (or your org’s equivalent).

**`string` route parameters:**

Several endpoints bind identifiers as **`[FromRoute] string`** (not **`Guid`**) even when values are semantically UUIDs. Controllers should log those strings only as **`LogSanitizer.Sanitize(...)`**; the **model pack** above should clear **`cs/log-forging`** for those call sites.

| Location | What is logged | Parameter type in code |
| -------- | -------------- | ------------------------ |
| **`ArchLucid.Api/Controllers/Authority/RunsController.cs`** | **`runId`** in execute / replay / determinism / commit / detail paths | **`[FromRoute] string runId`** |
| **`ArchLucid.Api/Controllers/Authority/AnalysisReportsController.cs`** | **`runId`** in analysis / export logs | **`[FromRoute] string runId`** |
| **`ArchLucid.Api/Controllers/Governance/GovernanceController.cs`** | **`approvalRequestId`** in approve / reject logs | **`[FromRoute] string approvalRequestId`** |

If CodeQL still flags a line after **`LogSanitizer.Sanitize`**, verify the extension pack is loaded (see workflow **`config-file`**), route **`LogWarning`** + user string through **`LogWarningWithSanitizedUserArg`** (see § boxing above), or refactor to **`Guid`** + **`{param:guid}`** and dismiss value-type cases per above.

### Coordinator lease names and `cs/exposure-of-sensitive-information`

**`cs/exposure-of-sensitive-information`** may treat well-known **coordinator lease strings** (for example **`HostElectionLeaseNames.TrialLifecycleEmailPolling`**) as private when they flow into **`ILogger`**, even though they are **stable operational keys** (not passwords, tokens, or PII).

**Mitigation:** route lease/instance logs through **`SanitizedLoggerHostLeaderElectionExtensions`** (sanitization + **`// codeql`** on the **`ILogger`** sink in Core). The built-in query may still anchor SARIF at the **`HostLeaderElectionCoordinator`** argument line passing **`leaseName`**; place **`// codeql[cs/exposure-of-sensitive-information]`** immediately **above** those extension calls when that happens (or dismiss in code scanning with: *operational lease identifiers, sanitized; not credentials*).

| Location | Notes |
| -------- | ----- |
| **`ArchLucid.Host.Core/Hosted/HostLeaderElectionCoordinator.cs`** | Use **`SanitizedLoggerHostLeaderElectionExtensions`** at call sites. Core holds sink suppressions; if CodeQL highlights the **`leaseName`** argument line here, duplicate **`// codeql[cs/exposure-of-sensitive-information]`** on the line immediately **above** the extension call (**`TrialLifecycleEmailPolling`** and peers are operational keys only). |


---

## Related documents

- [`SECURITY.md`](SECURITY.md) — CWE-117 policy and **`LogSanitizer`** usage  
- [`.github/workflows/codeql.yml`(../../.github/workflows/codeql.yml) — CodeQL workflow configuration  
