> **Scope:** Reference architecture request payloads (JSON) aligned with `POST /v1/architecture/request` (`ArchitectureRequest`) for pilots and demos.

Copy any template JSON file below into `POST /v1/architecture/request` as the JSON body (after setting scope headers / auth). Descriptions meet the API minimum length; adjust `requestId` per tenant conventions.

| File | Summary |
|------|---------|
| [`standard-3-tier-web.json`](standard-3-tier-web.json) | Classic web + API + Azure SQL behind private networking |
| [`azure-serverless-api.json`](azure-serverless-api.json) | HTTP API on Azure Functions + Service Bus + Cosmos DB |
| [`azure-data-pipeline-batch.request.json`](azure-data-pipeline-batch.request.json) | Ingest / transform / curated zone on Azure Data Factory + ADLS |
