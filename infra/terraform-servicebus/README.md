# Terraform: Azure Service Bus for ArchLucid integration events

Provisions a **namespace**, **topic** (duplicate detection enabled), and **two default subscriptions** (`archlucid-worker`, `archlucid-external`) for JSON integration events published by the API/worker (`IIntegrationEventPublisher` / transactional outbox).

Optionally, enable **`enable_logic_app_governance_approval_subscription`** to add a **third** subscription whose **`$Default`** rule is a SQL filter on user property **`event_type`** so only `com.archlucid.governance.approval.submitted` is delivered — the intended trigger for the **`logic-app-governance-approval-routing`** Logic App (see `docs/CURSOR_PROMPTS_LOGIC_APPS.md` and `infra/terraform-logicapps/`).

## Security and networking

- Prefer **managed identity** + `IntegrationEvents:ServiceBusFullyQualifiedNamespace` over connection strings in production.
- Do **not** expose the namespace on the public internet when a private network exists: set `enable_private_endpoint = true` and pass the private endpoints subnet plus the `privatelink.servicebus.windows.net` DNS zone id from `infra/terraform-private` (add a `azurerm_private_dns_zone` for Service Bus there if not already present, and link it to the workload VNet).
- IAM: optional role assignments grant **Data Sender** to the API identity and **Data Sender + Data Receiver** to the worker (outbox drain + subscription consumer). When the governance Logic App subscription is enabled, pass **`governance_logic_app_managed_identity_principal_id`** to grant **Data Receiver** on the namespace for that identity (namespace scope today; tighten when subscription-scoped RBAC fits your landing zone).

## Application configuration

After apply, set:

| Setting | Source |
|--------|--------|
| `IntegrationEvents:ServiceBusFullyQualifiedNamespace` | `namespace_fqdn` output |
| `IntegrationEvents:QueueOrTopicName` | `topic_name` output |
| `IntegrationEvents:SubscriptionName` | `worker_subscription_name` output (worker only, when `ConsumerEnabled` is true) |
| `IntegrationEvents:TransactionalOutboxEnabled` | `true` when using SQL storage (see host validation rules) |

## Usage

```bash
cd infra/terraform-servicebus
terraform init
cp terraform.tfvars.example terraform.tfvars
# edit tfvars
terraform plan
terraform apply
```

Run `terraform fmt` before commit.
