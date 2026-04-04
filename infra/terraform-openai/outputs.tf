output "openai_consumption_budget_id" {
  description = "Resource id of the OpenAI / Cognitive Services consumption budget when enable_openai_consumption_budget is true; otherwise null."
  value       = try(azurerm_consumption_budget_resource_group.openai[0].id, null)
}
