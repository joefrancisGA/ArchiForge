-- Add context snapshot reference to runs
ALTER TABLE ArchitectureRuns
    ADD ContextSnapshotId NVARCHAR(64) NULL;

