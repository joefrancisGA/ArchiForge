> **Scope:** Runbook: Advisory scan failures and schedule advance - full detail, tables, and links in the sections below.

# Runbook: Advisory scan failures and schedule advance

**Last reviewed:** 2026-04-16

## Symptoms

- Logs: **`Advisory scan failed for schedule {ScheduleId}`** from **`AdvisoryScanHostedService`**.
- **`AdvisoryScanExecution`** rows with **`Status=Failed`** or advisory UI showing repeated failures for a schedule.

## What should happen (v1)

**`AdvisoryScanRunner`** records execution status and **still advances** the schedule’s next run (failures do not block cadence indefinitely). Verify this matches product expectations before changing code.

## Triage

1. **Correlation:** Match **`ScheduleId`** / **`ExecutionId`** in logs to **`AdvisoryScanSchedule`** and latest **`AdvisoryScanExecution`**.
2. **Scope:** Confirm **`AmbientScopeContext`** during the run matches tenant/workspace/project on the schedule (mis-scoped governance or repos return empty or wrong data).
3. **Downstream:** Check SQL connectivity, **`IAdvisoryScanRunner`** dependencies (governance load, alert evaluation, digest persistence). Alert or digest channel failures may be isolated — see digest attempt history.

## Mitigation

- **Bad data:** Fix schedule CRON / timezone (**`SimpleScanScheduleCalculator`** uses UTC); see **`docs/TEST_STRUCTURE.md`** for schedule tests.
- **Transient SQL:** Retry is host-level (next poll interval). For persistent DB errors, fix connection string / firewall / pool limits.
- **Code defects:** Capture stack trace; reproduce with **`AdvisorySchedulingController.RunNow`** in a lower environment if exposed.

## Escalation

If schedules drift or duplicate executions appear across multiple API instances, treat as **scheduler concurrency** debt (v1 accepts occasional overlap) and plan distributed locking or lease-based leadership.
