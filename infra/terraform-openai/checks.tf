check "openai_consumption_budget_requires_resource_group" {
  assert {
    condition     = !var.enable_openai_consumption_budget || length(trimspace(var.openai_consumption_budget_resource_group_id)) > 0
    error_message = "With enable_openai_consumption_budget = true, set openai_consumption_budget_resource_group_id to the full ARM id of the resource group containing your Azure OpenAI account(s)."
  }
}

check "openai_consumption_budget_contact_channel" {
  assert {
    condition = !var.enable_openai_consumption_budget || (
      length(var.openai_consumption_budget_contact_emails) > 0 ||
      length(var.openai_consumption_budget_contact_roles) > 0
    )
    error_message = "With enable_openai_consumption_budget = true, set openai_consumption_budget_contact_emails and/or a non-empty openai_consumption_budget_contact_roles list."
  }
}

check "openai_consumption_budget_account_ids_nonempty_strings" {
  assert {
    condition = length(var.openai_consumption_budget_account_resource_ids) == 0 || alltrue([
      for id in var.openai_consumption_budget_account_resource_ids : length(trimspace(id)) > 0
    ])
    error_message = "openai_consumption_budget_account_resource_ids must not contain empty strings; omit the list or supply full Cognitive Services account ARM ids."
  }
}
