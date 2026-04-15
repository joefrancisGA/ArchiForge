workspace "ArchLucid" "ArchLucid C4 model (high level)" {

    model {
        operator = person "Operator" "Uses the operator UI or CLI to run authority workflows."
        automation = person "Automation" "CI/CD or scripts calling the REST API."

        archlucid = softwareSystem "ArchLucid" "Architecture authority: runs, manifests, governance, advisory, retrieval." {
            api = container "ArchLucid.Api" "ASP.NET Core REST API, OpenAPI, auth, OTel." "C# / .NET"
            worker = container "ArchLucid.Worker" "Background: outboxes, advisory, indexing." "C# / .NET"
            ui = container "archlucid-ui" "Next.js operator shell (proxies to API)." "TypeScript / React"
            database = container "SQL Server" "Authority + coordinator relational state, audit, outboxes." "Azure SQL"
            blob = container "Blob storage" "Artifacts, agent trace payloads (optional)." "Azure Blob"
            bus = container "Service Bus" "Integration events (optional)." "Azure Service Bus"
        }

        openai = softwareSystem "Azure OpenAI" "LLM completions and embeddings (optional / real mode)." "External"
        entra = softwareSystem "Microsoft Entra ID" "JWT issuance for production auth." "External"

        operator -> archlucid.ui "HTTPS"
        automation -> archlucid.api "HTTPS / API key"
        archlucid.ui -> archlucid.api "HTTPS (scoped headers)"
        archlucid.api -> archlucid.database "T-SQL (Dapper)"
        archlucid.worker -> archlucid.database "T-SQL"
        archlucid.api -> archlucid.blob "Managed identity / private endpoint"
        archlucid.worker -> archlucid.blob "Managed identity / private endpoint"
        archlucid.api -> archlucid.bus "Publish (optional)"
        archlucid.worker -> archlucid.bus "Publish / consume (optional)"
        archlucid.api -> openai "HTTPS (real agent mode)"
        archlucid.worker -> openai "HTTPS (indexing embeddings)"
        archlucid.api -> entra "JWT validation"
    }

    views {
        systemContext archlucid "SystemContext" {
            include *
            autolayout lr
        }

        container archlucid "Containers" {
            include *
            autolayout lr
        }

        theme default
    }
}
