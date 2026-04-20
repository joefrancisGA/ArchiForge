> **Scope:** Project and documentation consolidation (proposal) - full detail, tables, and links in the sections below.

# Project and documentation consolidation (proposal)

**Status:** Proposal (2026-04-20)  
**Goal:** Reduce duplicate entry points and competing “start here” narratives without deleting historical context.

## What to consolidate

1. **Single navigation spine:** Treat [docs/ARCHITECTURE_INDEX.md](ARCHITECTURE_INDEX.md) as the doc graph root; [docs/START_HERE.md](START_HERE.md) stays a short persona router only.
2. **Buyer narrative:** One outward story — [EXECUTIVE_SPONSOR_BRIEF.md](EXECUTIVE_SPONSOR_BRIEF.md); positioning pages link in, not restate ([go-to-market/POSITIONING.md](go-to-market/POSITIONING.md)).
3. **Platform clarity:** Azure-first operations are explicit ([ADR 0020](adr/0020-azure-primary-platform-permanent.md)); Terraform and runbooks stay aligned with IaC in `infra/`.

## What not to do

- Do not rewrite archived docs; supersede with pointers.
- Do not edit historical SQL migrations (001–028); use new migrations and `ArchLucid.Persistence/Scripts/ArchLucid.sql` for DDL alignment.

## Next steps (incremental)

- When adding a new “hub” doc, link it from `ARCHITECTURE_INDEX.md` in one line.
- Prefer extending an existing Day-1 doc over adding a parallel onboarding file.
