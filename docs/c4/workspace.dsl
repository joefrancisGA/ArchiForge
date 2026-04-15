/*
  ArchLucid — Structurizr DSL (C4). Render with Structurizr Lite or CLI.
  See docs/c4/README.md.
*/
workspace "ArchLucid" "Architecture decision record — system and containers" {

    model {
        operator = person "Operator" "Uses the operator UI and CLI for runs, governance, and forensics."
        integrator = person "Integrator" "Calls the versioned HTTP API with tenant scope and auth."

        archlucid = softwareSystem "ArchLucid" "Authority pipeline: ingest → graph → findings → manifests → SQL persistence." {
            api = container "ArchLucid API" ".NET HTTP host — /v1 routes, health, OpenAPI." "ASP.NET Core"
            worker = container "ArchLucid Worker" "Background jobs, outbox, indexing, archival." "ASP.NET Core Worker"
            ui = container "Operator UI" "Next.js operator shell." "JavaScript"
            database = container "SQL Server" "Runs, snapshots, manifests, audit, outbox." "SQL Server"
        }

        operator -> ui "Operates"
        operator -> archlucid.api "HTTPS (API key / JWT)"
        integrator -> archlucid.api "HTTPS JSON / OpenAPI"
        archlucid.ui -> archlucid.api "HTTPS"
        archlucid.api -> archlucid.database "Dapper / transactions"
        archlucid.worker -> archlucid.database "Dapper / transactions"
    }

    views {
        systemContext archlucid "SystemContext" {
            include *
            autolayout lr
        }

        container archlucid "Containers" {
            include *
            autolayout tb
        }

        theme default
    }
}
