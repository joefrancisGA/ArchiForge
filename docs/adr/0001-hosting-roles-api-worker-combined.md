> **Scope:** ADR 0001: Hosting roles (Api, Worker, Combined) - full detail, tables, and links in the sections below.

# ADR 0001: Hosting roles (Api, Worker, Combined)

- **Status:** Accepted
- **Date:** 2026-04-04

## Context

ArchLucid runs HTTP APIs, background jobs (advisory, outbox, durable exports), and optional combined dev hosts. Operators need clear deployment units.

## Decision

Use configuration **`Hosting:Role`**: **`Api`**, **`Worker`**, or **`Combined`** (default for local dev). Split processes in production for blast-radius and scaling.

## Consequences

- **Positive:** Scale API and worker independently; validate production SQL + RLS for both paths.
- **Negative:** More Terraform/Container Apps definitions and operational runbooks.

## Links

- `docs/DEPLOYMENT_TERRAFORM.md`
- `ArchLucid.Host.Core.Hosting.HostingRoleResolver`
