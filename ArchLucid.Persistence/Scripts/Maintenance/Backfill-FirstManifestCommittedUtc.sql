/*
  One-shot maintenance: backfill dbo.Tenants.TrialFirstManifestCommittedUtc for tenants that already have at least one
  golden manifest row but never received the pin (e.g. historical commits before the writer applied to all tiers).

  Idempotent: only updates rows where TrialFirstManifestCommittedUtc IS NULL.

  Anchor: MIN(dbo.GoldenManifests.CreatedUtc) per TenantId — persisted commit time for the first golden manifest row.

  NOT a migration — run manually against the target database after deploy verification.
*/

SET NOCOUNT ON;

IF OBJECT_ID(N'dbo.Tenants', N'U') IS NULL
   OR OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NULL
    RETURN;

UPDATE t
SET TrialFirstManifestCommittedUtc = TODATETIMEOFFSET(x.FirstUtc, 0)
FROM dbo.Tenants AS t
INNER JOIN (
    SELECT TenantId,
           MIN(CreatedUtc) AS FirstUtc
    FROM dbo.GoldenManifests
    GROUP BY TenantId
) AS x
    ON x.TenantId = t.Id
WHERE t.TrialFirstManifestCommittedUtc IS NULL;
