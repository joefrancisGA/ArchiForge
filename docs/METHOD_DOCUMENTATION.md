# Method and API documentation (XML comments)

We document public and internal decisioning APIs with **C# XML documentation comments** (`///`) so IntelliSense, DocFX, and future reference docs stay accurate.

Documentation is being added **in pieces** (namespace / feature areas). This file describes what to include; see the **Piece tracker** at the bottom.

## What to include (the more detailed, the better)

For each **type**, add a `<summary>` and, when helpful, `<remarks>` covering:

- **Semantics** — what the type represents, invariants, and how it fits the domain.
- **Threading / lifetime** — e.g. “registered scoped in DI”, “immutable after publish”, “do not mutate shared static options”.

For each **method** (including interface methods and explicit interface implementations), add:

| Element | Purpose |
|--------|---------|
| `<summary>` | What the method does in one or two sentences. |
| `<remarks>` | **Why** it exists, ordering guarantees, edge cases, interaction with persistence or HTTP. |
| `<param name="...">` | Meaning and valid range for each parameter. |
| `<returns>` | Meaning of the return value, including null / empty collection semantics. |
| `<exception cref="...">` | Documented exceptions (do not document every possible failure if unknown). |
| `<seealso cref="..."/>` | Related types or entry points (e.g. HTTP controller, DI registration). |

### Call sites (“where is it called from?”)

Roslyn does not auto-generate caller lists in XML. In `<remarks>`, list **primary callers** when that helps operators:

- **API:** `ArchiForge.Api.Controllers.*` (e.g. `PolicyPacksController`, `GovernanceResolutionController`).
- **Application services:** `ArchiForge.Api.Services.*`.
- **Persistence:** `ArchiForge.Persistence.*` (repositories, runners).
- **Tests:** `*.Tests` projects.

Keep this **curated** (main orchestration paths only) so it does not go stale on every refactor. Prefer `<seealso cref="PolicyPacksController"/>` over long prose when the type name is self-explanatory.

## Conventions

- Use `<see cref="Type"/>` and `<c>code</c>` for identifiers.
- For JSON property names, reference `<see cref="PolicyPackContentDocument"/>` and `JsonPropertyName` where relevant.
- **Internal** helpers: still document with `///`; use `<remarks>Called from <see cref="ResolveAsync"/> only.</remarks>` when true.

## Piece tracker (governance)

| Piece | Area | Status |
|-------|------|--------|
| 1 | `ArchiForge.Decisioning/Governance/**` (`PolicyPacks/*`, `Resolution/*`, governance filters) | Documented: interfaces, services, DTOs, static filters, JSON options |
| 2 | Policy pack HTTP + app service + SQL/in-memory assignment & pack repos | `PolicyPacksController`, `GovernanceResolutionController`, `PolicyPacksAppService`, `PolicyPackRequests`, `DapperPolicyPack*`, `InMemoryPolicyPackAssignmentRepository` |
| 3 | Policy pack FluentValidation, compliance/alert consumers, advisory scan | `PolicyPackRequestValidationRules`, `*PolicyPack*Validator`, `PolicyFilteredComplianceRulePackProvider`, `AlertService`, `CompositeAlertService`, `IAlertService`, `ICompositeAlertService`, `AdvisoryScanRunner` |
| 4 | Alert evaluation context + scope resolution (governance-related call chain) | `AlertEvaluationContext`, `AlertEvaluationContextFactory`, `ScopeContext`, `IScopeContextProvider`, `HttpScopeContextProvider`, `AlertsController`, `IAlertEvaluator`, `AlertEvaluator`, `IAlertSimulationContextProvider`, `AlertSimulationContextProvider` |
| 5 | Alert persistence + composite pipeline + delivery | `IAlertRecordRepository`, `DapperAlertRecordRepository`, `InMemoryAlertRecordRepository`, `IAlertRuleRepository`, `DapperAlertRuleRepository`, `InMemoryAlertRuleRepository`, `ICompositeAlertRuleRepository`, `DapperCompositeAlertRuleRepository`, `InMemoryCompositeAlertRuleRepository`, `AlertMetricSnapshot`, `IAlertMetricSnapshotBuilder`, `AlertMetricSnapshotBuilder`, `ICompositeAlertRuleEvaluator`, `CompositeAlertRuleEvaluator`, `IAlertSuppressionPolicy`, `AlertSuppressionPolicy`, `AlertSuppressionDecision`, `IAlertDeliveryDispatcher`, `AlertDeliveryDispatcher`, `AlertEvaluationOutcome`, `CompositeAlertEvaluationResult` |
| 6 | Alert delivery surface + core alert models | `IAlertDeliveryChannel`, `AlertDeliveryPayload`, `AlertSeverityComparer`, `AlertRoutingChannelType`, `AlertDeliveryAttemptStatus`, `AlertEmailDeliveryChannel`, `AlertSlackWebhookDeliveryChannel`, `AlertTeamsWebhookDeliveryChannel`, `AlertOnCallWebhookDeliveryChannel`, `IAlertRoutingSubscriptionRepository`, `IAlertDeliveryAttemptRepository`, `AlertRoutingSubscription`, `AlertDeliveryAttempt`, `DapperAlertRoutingSubscriptionRepository`, `DapperAlertDeliveryAttemptRepository`, `InMemoryAlertRoutingSubscriptionRepository`, `InMemoryAlertDeliveryAttemptRepository`, `AlertRoutingSubscriptionsController`, `AlertSeverity`, `AlertRecord`, `AlertRule`, `CompositeAlertRule`, `AlertRuleCondition` |
| 7 | Alert vocabulary, simulation & tuning | `AlertRuleType`, `AlertStatus`, `AlertActionType`, `AlertActionRequest`, `CompositeOperator`, `AlertConditionOperator`, `AlertMetricType`, `CompositeDedupeScope`, `IRuleSimulationService`, `RuleSimulationRequest`/`Result`, `SimulatedAlertOutcome`, `RuleCandidateComparisonRequest`/`Result`, `RuleSimulationService`, `AlertSimulationController`, `IThresholdRecommendationService`, `ThresholdRecommendationService`, `IAlertNoiseScorer`, `AlertNoiseScorer`, `ThresholdRecommendationRequest`/`Result`, `ThresholdCandidate`, `ThresholdCandidateEvaluation`, `NoiseScoreBreakdown`, `AlertTuningController`, `ThresholdRecommendationRequestValidator` |
| 8 | Alert rule CRUD + simulation validators + advisory HTTP + overview doc | `AlertRulesController`, `CompositeAlertRulesController`, `AlertRuleBodyValidator`, `CompositeAlertRuleBodyValidator`, `RuleSimulationRequestValidator`, `RuleCandidateComparisonRequestValidator`, `AdvisoryController`, `AdvisorySchedulingController`, `docs/ALERTS.md` |
| 9 | Advisory digest delivery + learning + scope debug | `DigestSubscriptionsController`, `IDigestSubscriptionRepository`, `IDigestDeliveryAttemptRepository`, `DigestSubscription`, `DigestDeliveryAttempt`, `DigestDeliveryPayload`, `DigestDeliveryStatus`, `DigestDeliveryChannelType`, `IDigestDeliveryDispatcher`, `IDigestDeliveryChannel`, `DigestDeliveryDispatcher`, `DapperDigestSubscriptionRepository`, `DapperDigestDeliveryAttemptRepository`, `RecommendationLearningController`, `IRecommendationLearningService`, `RecommendationLearningService`, `IRecommendationLearningAnalyzer`, `RecommendationLearningProfile`, `RecommendationOutcomeStats`, `ScopeDebugController` |
| 10 | Recommendations + architecture digests (persist, generate, HTTP) | `IRecommendationRepository`, `DapperRecommendationRepository`, `InMemoryRecommendationRepository`, `IRecommendationWorkflowService`, `RecommendationWorkflowService`, `RecommendationRecord`, `RecommendationStatus`, `RecommendationActionType`, `RecommendationActionRequest`, `IRecommendationGenerator`, `RecommendationGenerator`, `IRecommendationFeedbackAnalyzer`, `RecommendationFeedbackAnalyzer`, `IImprovementAdvisorService`, `ImprovementAdvisorService`, `ArchitectureDigest`, `IArchitectureDigestBuilder`, `ArchitectureDigestBuilder`, `IArchitectureDigestRepository`, `DapperArchitectureDigestRepository`, `InMemoryArchitectureDigestRepository`, `RecommendationRecordResponse`, `AdvisoryController` (method/param detail), `AdvisorySchedulingController` (digest endpoints) |
| 11 | Advisory scan schedules + executions + next-run calc + improvement signals/plan DTOs | `IAdvisoryScanRunner`, `AdvisoryScanSchedule`, `AdvisoryScanExecution`, `IScanScheduleCalculator`, `SimpleScanScheduleCalculator`, `IAdvisoryScanScheduleRepository`, `DapperAdvisoryScanScheduleRepository`, `InMemoryAdvisoryScanScheduleRepository`, `IAdvisoryScanExecutionRepository`, `DapperAdvisoryScanExecutionRepository`, `InMemoryAdvisoryScanExecutionRepository`, `AdvisoryScanHostedService`, `ImprovementPlan`, `ImprovementSignal`, `ImprovementRecommendation`, `IImprovementSignalAnalyzer`, `ImprovementSignalAnalyzer`, `ImprovementPlanResponse`, `ImprovementRecommendationResponse`, `AdvisorySchedulingController` (schedule/execution/run-now detail) |

After each piece, run `dotnet build` and fix any CS1573/CS1591 warnings if the project enforces documentation.
