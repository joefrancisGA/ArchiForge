# case-01

Terraform `show -json` with a simple Azure resource group and storage account; expect two `TopologyResource` canonical objects with `terraformType` and `tf.*` properties.

Deterministic parser: `TerraformShowJsonInfrastructureDeclarationParser` (no LLM).
