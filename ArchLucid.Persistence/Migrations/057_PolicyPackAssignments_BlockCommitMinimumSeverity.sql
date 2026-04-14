-- 057: Configurable minimum severity for pre-commit governance gate
IF COL_LENGTH('dbo.PolicyPackAssignments', 'BlockCommitMinimumSeverity') IS NULL
BEGIN
    ALTER TABLE dbo.PolicyPackAssignments
        ADD BlockCommitMinimumSeverity INT NULL;
END
