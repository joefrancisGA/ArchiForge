# GitHub Action: ArchLucid manifest delta — PR comment

Composite action that fetches the structured golden-manifest delta (**`GET /v1/compare`**) for two committed runs and **posts or idempotently updates a single sticky Markdown comment** on the pull request.

The delta Markdown is rendered by the sibling [`github-action-manifest-delta`](../github-action-manifest-delta/) script — both actions therefore share the **same** Markdown shape (single source of truth). This action's job is only the GitHub-side delivery: list comments, find the sticky marker, PATCH or POST.

## Inputs

| Name | Required | Default | Description |
| --- | --- | --- | --- |
| `api-base-url` | yes | — | API origin without trailing slash. |
| `api-token` | yes | — | `X-Api-Key` value with **ReadAuthority**. Reuse `secrets.ARCHLUCID_READONLY_API_KEY`. |
| `base-run-id` | yes | — | Baseline committed run id (GUID string). |
| `target-run-id` | yes | — | Candidate committed run id (GUID string). |
| `pr-number` | yes | — | PR number to comment on (e.g. `${{ github.event.pull_request.number }}`). |
| `repository` | no | `${{ github.repository }}` | `owner/repo` slug. |
| `github-token` | yes | — | Token for `gh api`. The default `secrets.GITHUB_TOKEN` works if the job has `permissions: pull-requests: write`. |
| `marker` | no | `<!-- archlucid:manifest-delta -->` | HTML-comment sticky marker. Override only when one PR receives multiple delta comments (e.g. one per tenant). |
| `operator-compare-url-template` | no | `''` | Optional deep link template using `{baseRunId}` and `{targetRunId}` placeholders. |

## Sticky behaviour

The action prepends a hidden HTML-comment marker to the body. On every run it:

1. Lists the PR's existing comments via `gh api repos/$OWNER/$REPO/issues/$PR/comments` (`--paginate` so PRs with > 30 comments still resolve).
2. Looks for one whose body **contains** the marker.
3. **PATCH**es it in place when one exists, **POST**s a new one otherwise.

Re-runs of the workflow rewrite the same comment instead of stacking duplicates. Different markers (one per tenant, etc.) are independent.

## Usage

See **[`docs/integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md`](../../docs/integrations/GITHUB_ACTION_MANIFEST_DELTA_PR_COMMENT.md)** for the full contract and **[`.github/workflows/example-manifest-delta-pr-comment.yml`](../../.github/workflows/example-manifest-delta-pr-comment.yml)** for a copy-pasteable workflow.

## Tests

Pure-function smoke test for the sticky upsert (mocks the `gh` client; never invokes `gh` or hits the GitHub API):

```bash
node --test integrations/github-action-manifest-delta-pr-comment/post-pr-comment.test.mjs
```
