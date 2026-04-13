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

### False positives

Treat as a **false positive** when the logged parameter is a **value type** bound from **`[FromRoute]`** (or otherwise not arbitrary attacker-controlled string content), e.g. **`Guid`**, **`int`**, **`DateTime`**. Their formatted output does not carry the same newline/control-character injection risk as arbitrary strings.

**Dismiss in the GitHub CodeQL / code scanning UI** with a reason along the lines of:

> False positive — value type cannot contain control characters.

(Adjust the note if the query specifically references `ToString()` on a value type.)

### Known alerts to triage (run / approval identifiers)

**Value-type binding (typical false positive for CWE-117):**

| Location | What is logged | Parameter type in code |
| -------- | -------------- | ------------------------ |
| **`ArchLucid.Api/Controllers/ExplanationController.cs`** | **`runId`** in provenance / explanation warnings | **`Guid`** with route template **`{runId:guid}`** |

Dismiss with: *False positive — value type cannot contain control characters* (or your org’s equivalent).

**`string` route parameters (tooling may still flag):**

Several endpoints bind identifiers as **`[FromRoute] string`** (not **`Guid`**) even when values are semantically UUIDs. CodeQL may therefore keep reporting **“log entries from user input”** for those parameters.

| Location | What is logged | Parameter type in code |
| -------- | -------------- | ------------------------ |
| **`ArchLucid.Api/Controllers/RunsController.cs`** | **`runId`** in execute / replay / determinism / commit / detail paths | **`[FromRoute] string runId`** |
| **`ArchLucid.Api/Controllers/AnalysisReportsController.cs`** | **`runId`** in analysis / export logs | **`[FromRoute] string runId`** |
| **`ArchLucid.Api/Controllers/GovernanceController.cs`** | **`approvalRequestId`** in approve / reject logs | **`[FromRoute] string approvalRequestId`** |

For these, either:

- Apply **`LogSanitizer.Sanitize(...)`** (safest for static analysis and plaintext sinks), or  
- Refactor to **`Guid`** + **`{param:guid}`** (code change; aligns types with the dismissal rationale), or  
- **Dismiss in CodeQL** only with an **explicit org rationale** (e.g. identifier validated as a GUID before use and no control characters possible in practice).

After dismissing, prefer **“false positive”** (or your org’s equivalent) where applicable so alerts do not churn unnecessarily.

---

## Related documents

- [`SECURITY.md`](SECURITY.md) — CWE-117 policy and **`LogSanitizer`** usage  
- [`.github/workflows/codeql.yml`](../.github/workflows/codeql.yml) — CodeQL workflow configuration  
