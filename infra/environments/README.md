# Environment tfvars sketches

Copy the **`*.example.tfvars`** files into each Terraform root as **`terraform.tfvars`** (or pass **`-var-file`** explicitly). Values are placeholders: replace subscription IDs, SKUs, regions, and secrets with your organization’s landing zone decisions.

| File | Intent |
|------|--------|
| `dev.example.tfvars` | Laptop / shared dev: optional private endpoints off, smaller SKUs. |
| `staging.example.tfvars` | Pre-prod: private data plane, monitoring on, matches production topology at smaller scale. |
| `prod.example.tfvars` | Production-oriented defaults: enable flags documented in each root’s `README.md`. |

**Security:** never commit real secrets or production connection strings. Use Azure Key Vault, pipeline secret stores, or `terraform.tfvars` excluded by `.gitignore` locally.
