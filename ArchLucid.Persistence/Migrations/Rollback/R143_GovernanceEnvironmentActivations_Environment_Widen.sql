/*
  Rollback 143: narrows Environment back to NVARCHAR(32).

  Unsafe if any row holds more than 32 characters — validate before running.
*/

IF OBJECT_ID(N'dbo.GovernanceEnvironmentActivations', N'U') IS NOT NULL
   AND EXISTS (
       SELECT 1
       FROM sys.columns AS c
       INNER JOIN sys.types AS t ON c.user_type_id = t.user_type_id
       WHERE c.object_id = OBJECT_ID(N'dbo.GovernanceEnvironmentActivations')
         AND c.name = N'Environment'
         AND t.name = N'nvarchar'
         AND c.max_length = 128)
BEGIN
    ALTER TABLE dbo.GovernanceEnvironmentActivations
        ALTER COLUMN Environment NVARCHAR(32) NOT NULL;
END;
GO
