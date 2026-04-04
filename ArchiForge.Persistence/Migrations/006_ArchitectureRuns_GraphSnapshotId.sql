-- Add graph snapshot reference to runs
ALTER TABLE ArchitectureRuns
    ADD GraphSnapshotId UNIQUEIDENTIFIER NULL;

