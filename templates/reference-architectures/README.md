> **Scope:** Reference architecture request payloads (JSON) aligned with `POST /v1/architecture/request` (`ArchitectureRequest`) for pilots and demos.

Copy any `.request.json` file below into `POST /v1/architecture/request` as the JSON body (after setting scope headers / auth). Descriptions meet the API minimum length; adjust `requestId` per tenant conventions.

| File | Summary |
|------|---------|
| [`azure-three-tier-web.request.json`](azure-three-tier-web.request.json) | Classic web + API + Azure SQL behind private networking |
| [`azure-serverless-api.request.json`](azure-serverless-api.request.json) | HTTP API on Azure Functions + Service Bus + Cosmos DB |
| [`azure-data-pipeline-batch.request.json`](azure-data-pipeline-batch.request.json) | Ingest / transform / curated zone on Azure Data Factory + ADLS |
