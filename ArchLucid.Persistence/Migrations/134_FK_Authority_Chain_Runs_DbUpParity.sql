/*
  Migration 134: Foreign keys — authority chain RunId -> dbo.Runs + GraphSnapshots -> ContextSnapshots.

  Purpose: DbUp deployments that never ran bootstrap ArchLucid.sql may miss these FKs; add them brownfield-safe.

  Idempotent: each FK is added only when absent and orphan rows would not violate it.

  ON DELETE omitted => NO ACTION (SQL Server default; matches ArchLucid.sql).

  Rollback: Rollback/R134_FK_Authority_Chain_Runs_DbUpParity.sql
*/

/* ---- ContextSnapshots -> Runs ---- */
IF OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_ContextSnapshots_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.ContextSnapshots AS cs
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = cs.RunId))
BEGIN
    ALTER TABLE dbo.ContextSnapshots ADD CONSTRAINT FK_ContextSnapshots_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

/* ---- GraphSnapshots -> ContextSnapshots, then Runs ---- */
IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GraphSnapshots_ContextSnapshots_ContextSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GraphSnapshots AS gs
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ContextSnapshots AS cs WHERE cs.SnapshotId = gs.ContextSnapshotId))
BEGIN
    ALTER TABLE dbo.GraphSnapshots ADD CONSTRAINT FK_GraphSnapshots_ContextSnapshots_ContextSnapshotId
        FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GraphSnapshots_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GraphSnapshots AS gs
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = gs.RunId))
BEGIN
    ALTER TABLE dbo.GraphSnapshots ADD CONSTRAINT FK_GraphSnapshots_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

/* ---- FindingsSnapshots -> Runs, ContextSnapshots, GraphSnapshots ---- */
IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.FindingsSnapshots AS f
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = f.RunId))
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.FindingsSnapshots AS f
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ContextSnapshots AS cs WHERE cs.SnapshotId = f.ContextSnapshotId))
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_ContextSnapshots_ContextSnapshotId
        FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.FindingsSnapshots AS f
        WHERE NOT EXISTS (SELECT 1 FROM dbo.GraphSnapshots AS gs WHERE gs.GraphSnapshotId = f.GraphSnapshotId))
BEGIN
    ALTER TABLE dbo.FindingsSnapshots ADD CONSTRAINT FK_FindingsSnapshots_GraphSnapshots_GraphSnapshotId
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);
END;
GO

/* ---- DecisioningTraces -> Runs ---- */
IF OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_DecisioningTraces_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.DecisioningTraces AS dt
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = dt.RunId))
BEGIN
    ALTER TABLE dbo.DecisioningTraces ADD CONSTRAINT FK_DecisioningTraces_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

/* ---- GoldenManifests -> Runs (+ chain prerequisites) ---- */
IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.Runs', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_Runs_RunId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GoldenManifests AS gm
        WHERE NOT EXISTS (SELECT 1 FROM dbo.Runs AS r WHERE r.RunId = gm.RunId))
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_Runs_RunId
        FOREIGN KEY (RunId) REFERENCES dbo.Runs (RunId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.ContextSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_ContextSnapshots_ContextSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GoldenManifests AS gm
        WHERE NOT EXISTS (SELECT 1 FROM dbo.ContextSnapshots AS cs WHERE cs.SnapshotId = gm.ContextSnapshotId))
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_ContextSnapshots_ContextSnapshotId
        FOREIGN KEY (ContextSnapshotId) REFERENCES dbo.ContextSnapshots (SnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.GraphSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_GraphSnapshots_GraphSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GoldenManifests AS gm
        WHERE NOT EXISTS (SELECT 1 FROM dbo.GraphSnapshots AS gs WHERE gs.GraphSnapshotId = gm.GraphSnapshotId))
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_GraphSnapshots_GraphSnapshotId
        FOREIGN KEY (GraphSnapshotId) REFERENCES dbo.GraphSnapshots (GraphSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.FindingsSnapshots', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_FindingsSnapshots_FindingsSnapshotId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GoldenManifests AS gm
        WHERE NOT EXISTS (SELECT 1 FROM dbo.FindingsSnapshots AS fs WHERE fs.FindingsSnapshotId = gm.FindingsSnapshotId))
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_FindingsSnapshots_FindingsSnapshotId
        FOREIGN KEY (FindingsSnapshotId) REFERENCES dbo.FindingsSnapshots (FindingsSnapshotId);
END;
GO

IF OBJECT_ID(N'dbo.GoldenManifests', N'U') IS NOT NULL
   AND OBJECT_ID(N'dbo.DecisioningTraces', N'U') IS NOT NULL
   AND NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = N'FK_GoldenManifests_DecisioningTraces_DecisionTraceId')
   AND NOT EXISTS (
        SELECT 1
        FROM dbo.GoldenManifests AS gm
        WHERE NOT EXISTS (SELECT 1 FROM dbo.DecisioningTraces AS dt WHERE dt.DecisionTraceId = gm.DecisionTraceId))
BEGIN
    ALTER TABLE dbo.GoldenManifests ADD CONSTRAINT FK_GoldenManifests_DecisioningTraces_DecisionTraceId
        FOREIGN KEY (DecisionTraceId) REFERENCES dbo.DecisioningTraces (DecisionTraceId);
END;
GO
