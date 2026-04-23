> **Scope:** Dev container for **ArchLucid contributors and internal engineers** - full detail, tables, and links in the sections below.

> **Spine doc:** [Five-document onboarding spine](../FIRST_5_DOCS.md). Read this file only if you have a specific reason beyond those five entry documents.

> **Audience banner — read first.** ArchLucid is a **SaaS** product. **Customers, evaluators, and sponsors never open the dev container, run Docker, or install SQL.** This document is for ArchLucid contributors who want a Cursor / VS Code dev-container loop. Customer entry points: **[`START_HERE.md`](../START_HERE.md)** "Audience split" and `archlucid.com`.

# Dev container

The **`.devcontainer/devcontainer.json`** uses the Microsoft **.NET 10** dev image plus **Node.js 22** (Operator UI). It is intended for editors that support the Dev Containers spec.

## Data plane dependencies

The container does not embed SQL Server. On the **host**, run:

```bash
docker compose up -d
```

That starts **SQL Server**, **Azurite**, and **Redis** per **`docker-compose.yml`**. From inside the dev container, connect using **`host.docker.internal`** (Windows/macOS Docker Desktop) or your LAN IP (Linux) with the published ports (**1433**, blob **10000**, etc.).

## Environment

- Set **`ConnectionStrings:ArchLucid`** in **`appsettings.Development.json`** or user secrets to point at the forwarded SQL instance.
- For the UI, set **`ARCHLUCID_API_BASE_URL`** in **`archlucid-ui/.env.local`** to the API URL reachable from the dev container network.
