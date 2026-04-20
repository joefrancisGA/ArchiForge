# Workflow `promotion-customer-notifications` (placeholder)

**Objective:** On production governance promotion (`com.archlucid.governance.promotion.activated` with user property `promotion_environment = prod`), fan out customer-facing email, Teams, and signed webhooks in parallel branches.

## Logic App host (Terraform)

Optional dedicated site: **`enable_promotion_customer_notify_logic_app`** + **`promotion_customer_notify_storage_account_name`** in **`infra/terraform-logicapps/`**. Wire **`promotion_customer_notify_logic_app_managed_identity_principal_id`** in Service Bus to output **`promotion_customer_notify_logic_app_principal_id`**, then enable **`enable_logic_app_promotion_prod_customer_subscription`** and re-apply.

**Service Bus:** enable `enable_logic_app_promotion_prod_customer_subscription` in `infra/terraform-servicebus/`. The API and outbox processor set **`promotion_environment`** on the message from the JSON `environment` field so SQL filters work without parsing the body. Trigger on output **`logic_app_promotion_prod_customer_subscription_name`**.

**Channel preferences (API):** `GET` / **`PUT /v1/notifications/customer-channel-preferences`** — see `CustomerNotificationChannelPreferencesController`; contracts `TenantNotificationChannelPreferencesResponse`, `TenantNotificationChannelPreferencesUpsertRequest` (PUT requires **Execute** / Operator+). **.NET client:** `CustomerChannelPreferencesAsync`, `CustomerChannelPreferencesPUTAsync` (regenerate via `dotnet build` on `ArchLucid.Api.Client` after OpenAPI snapshot updates).

**Still out of repo:** `workflow.json` and HMAC signing steps with `secureInput` / `secureOutput` (design in Portal / your CD pipeline).
