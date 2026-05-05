SET NOCOUNT ON;
GO

SET XACT_ABORT ON;
GO

/*
  143: Widen dbo.GovernanceEnvironmentActivations.Environment NVARCHAR(32) -> NVARCHAR(64).

        Architecture/governance templates accept labels beyond 32 chars; CI contract tests use a compact prefix + Guid ("N")
        for uniqueness on shared catalogs.

        Idempotent: skips when column is already wider than legacy 32 code units.
*/

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.columns AS c
       INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
       WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
         AND c.name = N'Environment'
         AND t.name = N'nvarchar'
         AND c.max_length > 0
         AND c.max_length < 128)
BEGIN
    ALTER TABLE dbo.GovernanceEnvironmentActivations
        ALTER COLUMN Environment NVARCHAR(64) NOT NULL;
END;
GO
