check "pilot_budget_positive" {
  assert {
    condition     = var.pilot_monthly_budget_usd > 0
    error_message = "pilot_monthly_budget_usd must be positive for FinOps sanity."
  }
}

check "pilot_sampling_sane" {
  assert {
    condition     = var.app_insights_sampling_percent >= 0 && var.app_insights_sampling_percent <= 100
    error_message = "app_insights_sampling_percent must be between 0 and 100."
  }
}
