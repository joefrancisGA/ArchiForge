# Architecture Review Gate for Pull Requests

**Pattern:** A CI job runs on every pull request, calls the ArchLucid V1 **architecture** API to create and execute a run from the PR title/body, waits until the run reaches a terminal state, checks **findings** for severity, posts a **Markdown** summary to the PR, and **fails the job** (blocking merge when branch protection is enabled) if any finding is at or above your configured floor (for example **Critical** only).

**Related (structured manifest delta in PRs):** For the shipped **compare / manifest delta** workflow that reuses a single Node script and posts sticky PR comments, see the repository’s [`integrations/github-action-manifest-delta/`](../../../integrations/github-action-manifest-delta/) and [`integrations/github-action-manifest-delta-pr-comment/`](../../../integrations/github-action-manifest-delta-pr-comment/) (same Markdown source of truth as Azure DevOps tasks). This recipe is **complementary**: it uses **create → execute → poll** and **findings-based** gating.

---

## Configuration

| Name | Required | Description |
|------|----------|-------------|
| `ARCHLUCID_API_URL` | Yes | HTTPS API base **without** a trailing slash, e.g. `https://api.contoso.com` |
| `ARCHLUCID_API_KEY` | Yes | Value for `X-Api-Key` (see [SECURITY.md](../../../docs/library/SECURITY.md) — store in GitHub/ADO secret store) |
| `ARCHLUCID_BLOCK_SEVERITY` | No | `critical` (default), `high`, `medium`, `low`, or `info` — the job **fails** if any finding is **at or above** this level |
| `ARCHLUCID_MAX_WAIT_SEC` | No | Poll budget waiting for a terminal `run.status` (default `1800`) |
| `ARCHLUCID_SYSTEM_NAME` | No | `systemName` in `POST /v1/architecture/request` (default `PR review gate`) |
| `ARCHLUCID_ENVIRONMENT` | No | `environment` string in the same request (default `pr-preview`) |
| `PR_TITLE` / `PR_BODY` | Yes (script) | Fed into the architecture request; `PR_BODY` must be **at least 10 characters** to satisfy API validation |
| `PR_NUMBER` / `REPO_SLUG` | No | Included in the generated comment footer (cosmetic) |

**API surface used (V1, shipped):**

- `POST /v1/architecture/request` — create run
- `POST /v1/architecture/run/{runId}/execute` — execute agent pipeline
- `GET /v1/architecture/run/{runId}` — poll until `run.status` is terminal (`ReadyForCommit`, `Committed`, or `Failed` as names; or enum values `4`–`6` as numbers in JSON)

Findings are read from `results[]..findings[]..severity` on the run detail payload.

---

## Shell script

Copy [`archlucid-pr-gate.sh`](./archlucid-pr-gate.sh) into your repository (for example `.github/scripts/archlucid-pr-gate.sh`), `chmod +x` it, and set the environment variables above. The script:

1. Creates a run with a short `requestId` and a description built from the PR title and body.  
2. Executes the run.  
3. Polls `GET /v1/architecture/run/{runId}` until `run.status` indicates **Ready for commit**, **Committed**, or **Failed**, or a timeout.  
4. Exits `1` if any finding is at/above `ARCHLUCID_BLOCK_SEVERITY`, else `0`.  
5. If `ARCHLUCID_POST_COMMENT_CMD` is set, runs it with `ARCHLUCID_COMMENT_FILE` pointing at a generated Markdown file (your recipe posts the PR comment).

**Example — GitHub Actions comment via `gh` (install GitHub CLI on the runner, `GH_TOKEN` with `pull_requests: write`):**

```bash
export ARCHLUCID_POST_COMMENT_CMD='gh pr comment "$PR_NUMBER" --body-file "$ARCHLUCID_COMMENT_FILE" --repo "$GITHUB_REPOSITORY"'
```

**Correlation / debugging:** Each failing HTTP call prints the response body. On the server, ASP.NET Core’s trace id is available in your API logs; send `X-Correlation-Id: <uuid>` (optional) on your `curl` calls in CI if you add the header in your environment — many deployments log it next to the same value returned in `problem+json` `extensions` (when present).

---

## GitHub Actions workflow (copy-paste)

Save as `.github/workflows/archlucid-pr-gate.yml`.

```yaml
# Runs an ArchLucid architecture review for each pull request and fails if findings exceed the severity floor.
name: archlucid-pr-gate
on:
  pull_request:
    types: [opened, synchronize, reopened, edited]
permissions:
  contents: read
  pull-requests: write
  statuses: write
jobs:
  review:
    runs-on: ubuntu-latest
    steps:
      - name: Check out
        uses: actions/checkout@v4
      - name: ArchLucid review gate
        env:
          ARCHLUCID_API_URL: ${{ secrets.ARCHLUCID_API_URL }}
          ARCHLUCID_API_KEY: ${{ secrets.ARCHLUCID_API_KEY }}
          ARCHLUCID_BLOCK_SEVERITY: critical
          PR_NUMBER: ${{ github.event.pull_request.number }}
          REPO_SLUG: ${{ github.repository }}
          PR_TITLE: ${{ github.event.pull_request.title }}
          PR_BODY: ${{ github.event.pull_request.body || 'No description provided — minimum length for API validation.' }}
          GITHUB_TOKEN: ${{ secrets.GITHUB_TOKEN }}
        run: |
          set -e
          B="$PR_BODY"
          if [ "${#B}" -lt 10 ]; then
            B="PR $PR_NUMBER — ${PR_TITLE}. (Body padded for ArchLucid request validation.)"
          fi
          export PR_BODY="$B"
          chmod +x .github/scripts/archlucid-pr-gate.sh
          export ARCHLUCID_POST_COMMENT_CMD='gh pr comment "$PR_NUMBER" --body-file "$ARCHLUCID_COMMENT_FILE" --repo "$GITHUB_REPOSITORY"'
          .github/scripts/archlucid-pr-gate.sh
```

> **Secrets:** In the repository, create secrets named **`ARCHLUCID_API_URL`** and **`ARCHLUCID_API_KEY`**. Do **not** log them.

> **Path:** Adjust the script path if you place `archlucid-pr-gate.sh` somewhere else. Install **jq** on `ubuntu-latest` (preinstalled) and the **GitHub CLI** if you use `gh` (`sudo apt-get install -y gh` in a step before the script, or use a `curl` + GitHub REST comment instead).

For **only** the **manifest compare delta** in Markdown (not findings gating), follow [`integrations/github-action-manifest-delta-pr-comment/`](../../../integrations/github-action-manifest-delta-pr-comment/) instead of this job.

---

## Azure DevOps Pipeline (YAML) (copy-paste)

Link a **variable group** (or pipeline variables) that defines **`ARCHLUCID_API_URL`** (plain) and **`ARCHLUCID_API_KEY`** (secret, **lock** the padlock in the UI). Place [`archlucid-pr-gate.sh`](./archlucid-pr-gate.sh) in your repo — for example `.ado/scripts/archlucid-pr-gate.sh` — and register the pipeline on **pull request** validation.

```yaml
# Azure DevOps — run ArchLucid review on each PR; fail the job if findings exceed the severity floor.
trigger: none
pr:
  - main
pool:
  vmImage: 'ubuntu-latest'
variables:
- group: archlucid-ci
steps:
- checkout: self
- bash: |
    set -e
    sudo apt-get update -yq
    sudo apt-get install -yq jq
    test -n "$ARCHLUCID_API_URL"
    test -n "$ARCHLUCID_API_KEY"
    export PR_NUMBER="$(System.PullRequest.PullRequestNumber)"
    export REPO_SLUG="$(Build.Repository.Name)"
    export PR_TITLE="PR ${PR_NUMBER}: $(Build.SourceBranchName) → $(System.PullRequest.TargetBranchName)"
    export PR_BODY="Automated review for pull request #${PR_NUMBER} in ${REPO_SLUG}. This text satisfies the API minimum length for the architecture request description field."
    export ARCHLUCID_BLOCK_SEVERITY=critical
    chmod +x .ado/scripts/archlucid-pr-gate.sh
    .ado/scripts/archlucid-pr-gate.sh
  displayName: ArchLucid PR gate
  env:
    ARCHLUCID_API_URL: $(ARCHLUCID_API_URL)
    ARCHLUCID_API_KEY: $(ARCHLUCID_API_KEY)
```

For a **proven** sticky **PR thread** in Markdown, reuse [`integrations/azure-devops-task-manifest-delta-pr-comment/`](../../../integrations/azure-devops-task-manifest-delta-pr-comment/); for this recipe’s `ARCHLUCID_POST_COMMENT_CMD` you can add a follow-up bash step that `curl`s Azure DevOps Git `pullRequestThreads` with `$(System.AccessToken)` and the Markdown file from a prior step, or use a custom script stored alongside `archlucid-pr-gate.sh`.

---

## Verify it works

1. **Health (unauthenticated, no V1 resource path):**  
   `curl -sS -o /dev/null -w "%{http_code}\n" "$ARCHLUCID_API_URL/health"`
2. **Authenticated V1 list (proves `X-Api-Key` and `Read` policy):**  
   `curl -sS -H "X-Api-Key: $ARCHLUCID_API_KEY" "$ARCHLUCID_API_URL/v1/architecture/runs?limit=1" | head -c 200; echo`
3. **One-off run detail (after you have a `runId` from a completed review):**  
   `curl -sS -H "X-Api-Key: $ARCHLUCID_API_KEY" "$ARCHLUCID_API_URL/v1/architecture/run/<runId>" | jq '.run.status, (.results[0].findings|length)'`

If (2) returns **200** and JSON, the secret and base URL are correct. If (2) is **401/403**, check API key, tenant, and the policy that maps your caller to `ReadAuthority` / `ExecuteAuthority` as required by the endpoints you call.

---

## Troubleshooting

| Symptom | What to check |
|--------|----------------|
| **HTTP 401/403** on `POST` | Create/execute need **Execute**-class policy for your key or JWT. Only **GET** `run` needs **Read**; confirm the same key can create runs in the UI or via a one-off `curl`. |
| **400** on create with “description” | `description` must be **≥ 10** characters; pad `PR_BODY` in CI as the GitHub example does. |
| **Timeout in script** | Increase `ARCHLUCID_MAX_WAIT_SEC` or pre-warm the API / workers so agent execution finishes within the window. |
| **No findings, expected some** | Findings are attached to `results` after a successful `execute` and depend on the agents and policies bound to the tenant; verify in the product UI for the same `runId`. |
| **Run failed (`status=Failed`)** | See API response body; check **correlation id** in your API **Application Insights** or stdout logs. |

HMAC and outbound webhooks are not used in this path — only **`X-Api-Key`**. For the **same** HMAC pattern used for **inbound** CloudEvents from ArchLucid, see [jira-webhook-bridge-recipe.md](../jira/jira-webhook-bridge-recipe.md) (`X-ArchLucid-Webhook-Signature`).

---

## Security and cost

- Store **`ARCHLUCID_API_KEY`** in the CI secret store; rotate on the same policy as other automation keys.  
- The script does **not** print the API key.  
- Each PR run incurs the **same** LLM/execute cost as an interactive run; scope `on:` and **paths** filters if you need to limit frequency.
