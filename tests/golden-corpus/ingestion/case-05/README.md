# case-05

Terraform JSON with a mix of valid resources and a **broken** resource entry (managed mode and name only — no `type` field). The parser silently skips invalid rows; valid resources are still materialized. Unknown provider types (e.g. a third-party `acme_server`) are still represented as `TopologyResource` with `terraformType` preserved.

No LLM. Log output is not part of the golden file (ILogger only).
