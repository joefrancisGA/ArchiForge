/*
  141: Set-based refresh for dbo.TenantHealthScores (TenantHealthScoringCalculator parity in T-SQL).

  Replaces per-tenant COUNT loops from the host with a single MERGE. Scoring formulas must stay aligned with
  ArchLucid.Core.CustomerSuccess.TenantHealthScoringCalculator (see ArchLucid.Core.Tests.CustomerSuccess.TenantHealthScoringCalculatorTests).
*/

CREATE OR ALTER PROCEDURE dbo.sp_TenantHealthScores_BatchRefresh
WITH EXECUTE AS OWNER
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH TenantPrimaryScope AS (
        SELECT
            t.Id AS TenantId,
            fw.WorkspaceId,
            fw.DefaultProjectId AS ProjectId
        FROM dbo.Tenants AS t
        OUTER APPLY (
            SELECT TOP (1)
                tw.Id AS WorkspaceId,
                tw.DefaultProjectId
            FROM dbo.TenantWorkspaces AS tw
            WHERE tw.TenantId = t.Id
            ORDER BY tw.CreatedUtc ASC
        ) AS fw
        WHERE fw.WorkspaceId IS NOT NULL
    ),
    Runs7d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(1) AS Cnt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.Runs AS r
            ON r.TenantId = tw.TenantId
           AND r.WorkspaceId = tw.WorkspaceId
           AND r.ScopeProjectId = tw.ProjectId
        WHERE r.ArchivedUtc IS NULL
          AND r.CreatedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME())
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Commits7d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(1) AS Cnt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.Runs AS r
            ON r.TenantId = tw.TenantId
           AND r.WorkspaceId = tw.WorkspaceId
           AND r.ScopeProjectId = tw.ProjectId
        INNER JOIN dbo.GoldenManifests AS gm ON gm.RunId = r.RunId
        WHERE r.ArchivedUtc IS NULL
          AND gm.CreatedUtc >= DATEADD(DAY, -7, SYSUTCDATETIME())
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Actors7d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(DISTINCT ae.ActorUserId) AS Cnt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.AuditEvents AS ae
            ON ae.TenantId = tw.TenantId
           AND ae.WorkspaceId = tw.WorkspaceId
           AND ae.ProjectId = tw.ProjectId
        WHERE ae.OccurredUtc >= DATEADD(DAY, -7, SYSUTCDATETIME())
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Breadth30d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(1) AS Cnt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.AuditEvents AS ae
            ON ae.TenantId = tw.TenantId
           AND ae.WorkspaceId = tw.WorkspaceId
           AND ae.ProjectId = tw.ProjectId
        WHERE ae.OccurredUtc >= DATEADD(DAY, -30, SYSUTCDATETIME())
          AND ae.EventType IN (
              N'ComparisonSummaryPersisted',
              N'ReplayExecuted',
              N'ProvenanceAccessed',
              N'ArtifactDownloaded',
              N'BundleDownloaded',
              N'RunExported',
              N'ArchitectureDocxExportGenerated',
              N'ReviewTrailAccessed',
              N'FindingsListAccessed')
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Signals90d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(1) AS TotalCt,
            SUM(CASE WHEN s.Disposition = N'Trusted' THEN 1 ELSE 0 END) AS TrustedCt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.ProductLearningPilotSignals AS s
            ON s.TenantId = tw.TenantId
           AND s.WorkspaceId = tw.WorkspaceId
           AND s.ProjectId = tw.ProjectId
        WHERE s.RecordedUtc >= DATEADD(DAY, -90, SYSUTCDATETIME())
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Gov30d AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            COUNT_BIG(1) AS Cnt
        FROM TenantPrimaryScope AS tw
        INNER JOIN dbo.GovernanceApprovalRequests AS g
            ON g.TenantId = tw.TenantId
           AND g.WorkspaceId = tw.WorkspaceId
           AND g.ProjectId = tw.ProjectId
        WHERE g.Status = N'Approved'
          AND g.ReviewedUtc >= DATEADD(DAY, -30, SYSUTCDATETIME())
        GROUP BY tw.TenantId, tw.WorkspaceId, tw.ProjectId
    ),
    Joined AS (
        SELECT
            tw.TenantId,
            tw.WorkspaceId,
            tw.ProjectId,
            ISNULL(r7.Cnt, 0) AS Runs7d,
            ISNULL(c7.Cnt, 0) AS Commits7d,
            ISNULL(a7.Cnt, 0) AS Actors7d,
            ISNULL(b30.Cnt, 0) AS Breadth30d,
            ISNULL(sig.TotalCt, 0) AS TotalSignals90d,
            ISNULL(sig.TrustedCt, 0) AS Trusted90d,
            ISNULL(g30.Cnt, 0) AS Gov30d
        FROM TenantPrimaryScope AS tw
        LEFT JOIN Runs7d AS r7
            ON r7.TenantId = tw.TenantId
           AND r7.WorkspaceId = tw.WorkspaceId
           AND r7.ProjectId = tw.ProjectId
        LEFT JOIN Commits7d AS c7
            ON c7.TenantId = tw.TenantId
           AND c7.WorkspaceId = tw.WorkspaceId
           AND c7.ProjectId = tw.ProjectId
        LEFT JOIN Actors7d AS a7
            ON a7.TenantId = tw.TenantId
           AND a7.WorkspaceId = tw.WorkspaceId
           AND a7.ProjectId = tw.ProjectId
        LEFT JOIN Breadth30d AS b30
            ON b30.TenantId = tw.TenantId
           AND b30.WorkspaceId = tw.WorkspaceId
           AND b30.ProjectId = tw.ProjectId
        LEFT JOIN Signals90d AS sig
            ON sig.TenantId = tw.TenantId
           AND sig.WorkspaceId = tw.WorkspaceId
           AND sig.ProjectId = tw.ProjectId
        LEFT JOIN Gov30d AS g30
            ON g30.TenantId = tw.TenantId
           AND g30.WorkspaceId = tw.WorkspaceId
           AND g30.ProjectId = tw.ProjectId
    ),
    Scored AS (
        SELECT
            j.TenantId,
            j.WorkspaceId,
            j.ProjectId,
            CAST(CASE
                WHEN (CASE
                          WHEN j.Runs7d = 0 THEN 1.0
                          WHEN j.Runs7d <= 2 THEN 2.0
                          WHEN j.Runs7d <= 5 THEN 3.0
                          WHEN j.Runs7d <= 9 THEN 4.0
                          ELSE 5.0
                      END
                      + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                      + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                      + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END) < 1.0
                    THEN 1.0
                WHEN (CASE
                          WHEN j.Runs7d = 0 THEN 1.0
                          WHEN j.Runs7d <= 2 THEN 2.0
                          WHEN j.Runs7d <= 5 THEN 3.0
                          WHEN j.Runs7d <= 9 THEN 4.0
                          ELSE 5.0
                      END
                      + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                      + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                      + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END) > 5.0
                    THEN 5.0
                ELSE ROUND(
                    CASE
                        WHEN j.Runs7d = 0 THEN 1.0
                        WHEN j.Runs7d <= 2 THEN 2.0
                        WHEN j.Runs7d <= 5 THEN 3.0
                        WHEN j.Runs7d <= 9 THEN 4.0
                        ELSE 5.0
                    END
                    + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                    + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                    + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END,
                    2)
            END AS DECIMAL(5, 2)) AS EngagementScore,
            CAST(CASE
                WHEN j.Breadth30d = 0 THEN 1.0
                WHEN j.Breadth30d <= 3 THEN 2.5
                WHEN j.Breadth30d <= 10 THEN 3.5
                WHEN j.Breadth30d <= 30 THEN 4.2
                ELSE 5.0
            END AS DECIMAL(5, 2)) AS BreadthScore,
            CAST(CASE
                WHEN j.TotalSignals90d <= 0 THEN 3.0
                WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.75 THEN 5.0
                WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.50 THEN 4.0
                WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.35 THEN 3.0
                WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.10 THEN 2.0
                ELSE 1.0
            END AS DECIMAL(5, 2)) AS QualityScore,
            CAST(CASE
                WHEN j.Gov30d = 0 THEN 2.0
                WHEN j.Gov30d <= 2 THEN 3.0
                WHEN j.Gov30d <= 5 THEN 4.0
                ELSE 5.0
            END AS DECIMAL(5, 2)) AS GovernanceScore,
            CAST(3.0 AS DECIMAL(5, 2)) AS SupportScore,
            CAST(ROUND(
                0.30 * (CASE
                    WHEN (CASE
                              WHEN j.Runs7d = 0 THEN 1.0
                              WHEN j.Runs7d <= 2 THEN 2.0
                              WHEN j.Runs7d <= 5 THEN 3.0
                              WHEN j.Runs7d <= 9 THEN 4.0
                              ELSE 5.0
                          END
                          + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                          + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                          + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END) < 1.0
                        THEN 1.0
                    WHEN (CASE
                              WHEN j.Runs7d = 0 THEN 1.0
                              WHEN j.Runs7d <= 2 THEN 2.0
                              WHEN j.Runs7d <= 5 THEN 3.0
                              WHEN j.Runs7d <= 9 THEN 4.0
                              ELSE 5.0
                          END
                          + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                          + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                          + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END) > 5.0
                        THEN 5.0
                    ELSE ROUND(
                        CASE
                            WHEN j.Runs7d = 0 THEN 1.0
                            WHEN j.Runs7d <= 2 THEN 2.0
                            WHEN j.Runs7d <= 5 THEN 3.0
                            WHEN j.Runs7d <= 9 THEN 4.0
                            ELSE 5.0
                        END
                        + CASE WHEN j.Commits7d > 0 THEN 0.4 ELSE 0.0 END
                        + CASE WHEN j.Actors7d >= 2 THEN 0.4 ELSE 0.0 END
                        + CASE WHEN j.Actors7d >= 4 THEN 0.2 ELSE 0.0 END,
                        2)
                END)
                + 0.20 * (CASE
                    WHEN j.Breadth30d = 0 THEN 1.0
                    WHEN j.Breadth30d <= 3 THEN 2.5
                    WHEN j.Breadth30d <= 10 THEN 3.5
                    WHEN j.Breadth30d <= 30 THEN 4.2
                    ELSE 5.0
                END)
                + 0.15 * (CASE
                    WHEN j.TotalSignals90d <= 0 THEN 3.0
                    WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.75 THEN 5.0
                    WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.50 THEN 4.0
                    WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.35 THEN 3.0
                    WHEN CAST(j.Trusted90d AS FLOAT) / CAST(j.TotalSignals90d AS FLOAT) >= 0.10 THEN 2.0
                    ELSE 1.0
                END)
                + 0.20 * (CASE
                    WHEN j.Gov30d = 0 THEN 2.0
                    WHEN j.Gov30d <= 2 THEN 3.0
                    WHEN j.Gov30d <= 5 THEN 4.0
                    ELSE 5.0
                END)
                + 0.15 * 3.0,
                2) AS DECIMAL(5, 2)) AS CompositeScore
        FROM Joined AS j
    )
    MERGE dbo.TenantHealthScores AS t
    USING Scored AS s
        ON t.TenantId = s.TenantId
    WHEN MATCHED THEN
        UPDATE SET
            WorkspaceId = s.WorkspaceId,
            ProjectId = s.ProjectId,
            EngagementScore = s.EngagementScore,
            BreadthScore = s.BreadthScore,
            QualityScore = s.QualityScore,
            GovernanceScore = s.GovernanceScore,
            SupportScore = s.SupportScore,
            CompositeScore = s.CompositeScore,
            UpdatedUtc = SYSUTCDATETIME()
    WHEN NOT MATCHED BY TARGET THEN
        INSERT (
            TenantId,
            WorkspaceId,
            ProjectId,
            EngagementScore,
            BreadthScore,
            QualityScore,
            GovernanceScore,
            SupportScore,
            CompositeScore,
            UpdatedUtc)
        VALUES (
            s.TenantId,
            s.WorkspaceId,
            s.ProjectId,
            s.EngagementScore,
            s.BreadthScore,
            s.QualityScore,
            s.GovernanceScore,
            s.SupportScore,
            s.CompositeScore,
            SYSUTCDATETIME());
END;
GO

IF DATABASE_PRINCIPAL_ID(N'ArchLucidApp') IS NOT NULL
   AND OBJECT_ID(N'dbo.sp_TenantHealthScores_BatchRefresh', N'P') IS NOT NULL
BEGIN
    GRANT EXECUTE ON OBJECT::dbo.sp_TenantHealthScores_BatchRefresh TO [ArchLucidApp];
END;
GO
