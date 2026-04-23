> **Scope:** Git server administrators installing server-side secret scanning; not client-side hook alternatives or ArchLucid application runtime security.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.


# Gitleaks — server-side pre-receive hook

**Goal:** Block pushes that introduce secrets into the canonical Git server **before** objects become reachable from default branches.

## Prerequisite

Install [gitleaks](https://github.com/gitleaks/gitleaks) on the Git host (Linux bare/self-managed `git` is the reference environment).

## Install

From the bare repository on the server:

```bash
chmod +x scripts/git-hooks/pre-receive-gitleaks.sh
ln -sf ../../scripts/git-hooks/pre-receive-gitleaks.sh hooks/pre-receive
```

Adjust the relative path if your bare repo layout differs.

## Behaviour

The hook reads `oldrev newrev refname` lines from stdin (standard Git `pre-receive`) and invokes `gitleaks git` with `--log-opts` scoped to commits introduced by each ref update. If gitleaks exits non-zero, the push is rejected.

## Client-side complement

Developers should also run `gitleaks detect --no-git -v` locally before pushing (see [../SECURITY.md](../library/SECURITY.md)). CI continues to run secret scanning on pull requests.

## Historical Stripe-shaped fixture

The literal `sk_test_12345678901234567890123456789012` appears only in **git history** as a retired test fixture (never a live Stripe secret). **Rotation:** no live credential was bound to this value; confirm no services reference it (none). Allow-list entry lives in [../../.gitleaks.toml](../../.gitleaks.toml). **Do not** remove the allow-list entry without rewriting history or CI will fail on legacy blobs.
