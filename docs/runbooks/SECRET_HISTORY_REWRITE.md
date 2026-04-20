> **Scope:** Runbook — Rewriting git history to evict a leaked secret - full detail, tables, and links in the sections below.

# Runbook — Rewriting git history to evict a leaked secret

> **Scope:** Documented procedure for evicting a previously-committed secret (API key, signing secret, connection string, OAuth token) from the **history** of `joefrancisGA/ArchLucid`. This is a **destructive, coordinated operation**: all clones must reset, all open PRs must rebase, and any in-flight CI run will fail mid-flight.
>
> **Status:** Draft (2026-04-20). Trigger when the marketability honesty boundary in `docs/MARKETABILITY_ASSESSMENT_2026_04_18.md` is acted on, or when any future leak is identified.

## Decide first — three orthogonal actions, do them in this order

1. **Rotate the credential at the provider.** Always do this first; history rewrites cannot un-leak a credential that has already been observed by anyone with access to the repository at any time. Treat the credential as compromised.
2. **Add the secret to detection lists.** Update `.gitleaks.toml` (or equivalent configuration consumed by `gacts/gitleaks` in CI) so future commits cannot reintroduce the same value or pattern.
3. **Then** — and only then — consider rewriting history. If the credential is rotated and CI prevents reintroduction, history rewriting is **optional** and primarily a brand/procurement-due-diligence concern.

## When **not** to rewrite

- The repository is public and the leak is older than 90 days. Assume the value is in someone's training set or scraper cache — rotate and move on.
- The leak is in a tag that has been published to GitHub Releases and downloaded by external users — those clones are already permanent.
- You cannot get an explicit OK from every active contributor to coordinate the reset — a forced push without coordination corrupts every clone.

## Pre-flight checklist (block until complete)

- [ ] Provider-side credential **rotated** and the old value confirmed inactive.
- [ ] Inventory of every commit containing the secret, captured to a private file (do **not** commit). Use `gitleaks detect --redact -v --no-git --source <path>` against a fresh clone.
- [ ] List of every active branch and PR. Notify each author with a window for them to push pending work.
- [ ] All scheduled CI workflows paused.
- [ ] Backup tarball of the repository taken to private storage.
- [ ] Maintainer agreement on the **commit message** to use for the rewrite (referenced from the team announcement).

## Procedure (using `git filter-repo`)

`git filter-repo` is the supported successor to `git filter-branch` and is faster, safer, and recommended by GitHub.

```bash
# 1. Fresh, mirror clone (so all refs are present, no working tree).
git clone --mirror git@github.com:joefrancisGA/ArchLucid.git archlucid-rewrite.git
cd archlucid-rewrite.git

# 2. Build a replacements file. One pattern per line:
#    literal-secret==>REDACTED
#    regex:sk_test_[A-Za-z0-9]{24,}==>REDACTED
cat > /tmp/replacements.txt <<'EOF'
sk_test_REPLACE_THIS_WITH_THE_REAL_PATTERN==>REDACTED
EOF

# 3. Dry-run preview.
git filter-repo --replace-text /tmp/replacements.txt --analyze
# Inspect ./filter-repo/analysis/ — confirm only expected files are touched.

# 4. Apply.
git filter-repo --replace-text /tmp/replacements.txt --force

# 5. Force-push every ref. THIS IS DESTRUCTIVE.
git push --force --all
git push --force --tags
```

## Post-flight checklist

- [ ] Every contributor has been told to:
  - Save any uncommitted work as a patch.
  - `git fetch --all && git reset --hard origin/<their-branch>` (or re-clone).
- [ ] Every open PR has been rebased onto the new history (PR authors do this).
- [ ] CI re-enabled.
- [ ] A short note added to `BREAKING_CHANGES.md` describing the rewrite (no secret content), the date, and the reason.
- [ ] An incident record opened in the existing security incident tracker.

## What CI prevents going forward

- `gitleaks` in Tier 0 of `.github/workflows/ci.yml` runs on every PR.
- `scripts/ci/check_pricing_single_source.py` is the working pattern for "this string belongs only here" — replicate it for any future secret-shaped value that **must** appear in tracked files (e.g., test fixtures with deliberately fake tokens).

## Related

- `.github/workflows/ci.yml` (Tier 0 — gitleaks)
- `docs/security/SYSTEM_THREAT_MODEL.md`
- `docs/MARKETABILITY_ASSESSMENT_2026_04_18.md` § honesty boundary
