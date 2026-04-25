# case-02

Terraform `show -json` with a VNet, subnet, and network security group. The NSG maps to `SecurityBaseline` (see `ResolveObjectTypeFromTerraformType`); the others map to `TopologyResource` (topology hints for the authority graph).

Deterministic parser: `TerraformShowJsonInfrastructureDeclarationParser` (no LLM).
