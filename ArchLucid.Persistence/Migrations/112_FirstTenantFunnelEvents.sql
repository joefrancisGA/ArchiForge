/*
  112: First-tenant onboarding telemetry funnel rows (Improvement 12).

  Holds per-tenant funnel events ONLY when the application feature flag
  Telemetry:FirstTenantFunnel:PerTenantEmission is on (owner-only flip per pending
  question 40 / docs/security/PRIVACY_NOTE.md §3.A). The schema is created
  unconditionally so a future flag flip does not require a follow-up migration; the
  row is empty until the owner enables per-tenant emission.

  Schema is intentionally minimal — no UserId, no IP, no UserAgent. The CHECK
  constraint on EventName pins the catalog to the six canonical values defined in
  ArchLucid.Core.Diagnostics.FirstTenantFunnelEventNames.

  RLS: not applied. The application never exposes raw rows — the API surface is
  aggregate-only (the Workbook reads from Application Insights customMetrics).
  Tenant-administrator export (subject access) goes through a separate forthcoming
  admin endpoint that filters by IScopeContextProvider tenant scope; until that
  endpoint exists, only the SQL admin can read rows directly.
*/
IF OBJECT_ID(N'dbo.FirstTenantFunnelEvents', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FirstTenantFunnelEvents
    (
        EventId      BIGINT           IDENTITY(1, 1) NOT NULL
            CONSTRAINT PK_FirstTenantFunnelEvents PRIMARY KEY CLUSTERED,
        TenantId     UNIQUEIDENTIFIER NOT NULL,
        EventName    NVARCHAR(64)     NOT NULL,
        OccurredUtc  DATETIME2(7)     NOT NULL
            CONSTRAINT DF_FirstTenantFunnelEvents_OccurredUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_FirstTenantFunnelEvents_EventName
            CHECK (EventName IN (
                N'signup',
                N'tour_opt_in',
                N'first_run_started',
                N'first_run_committed',
                N'first_finding_viewed',
                N'thirty_minute_milestone'
            )),
        CONSTRAINT FK_FirstTenantFunnelEvents_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id)
    );

    CREATE NONCLUSTERED INDEX IX_FirstTenantFunnelEvents_TenantId_OccurredUtc
        ON dbo.FirstTenantFunnelEvents (TenantId, OccurredUtc DESC);

    CREATE NONCLUSTERED INDEX IX_FirstTenantFunnelEvents_OccurredUtc
        ON dbo.FirstTenantFunnelEvents (OccurredUtc DESC);
END;
GO
