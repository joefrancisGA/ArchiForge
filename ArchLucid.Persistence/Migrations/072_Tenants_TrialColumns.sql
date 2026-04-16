/*
  072: Self-service trial metadata on dbo.Tenants (SaaS signup bootstrap).
*/
IF OBJECT_ID(N'dbo.Tenants', N'U') IS NOT NULL AND COL_LENGTH(N'dbo.Tenants', N'TrialStartUtc') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        TrialStartUtc      DATETIMEOFFSET   NULL,
        TrialExpiresUtc    DATETIMEOFFSET   NULL,
        TrialRunsLimit     INT              NULL,
        TrialRunsUsed      INT              NOT NULL CONSTRAINT DF_Tenants_TrialRunsUsed DEFAULT 0,
        TrialSeatsLimit    INT              NULL,
        TrialSeatsUsed     INT              NOT NULL CONSTRAINT DF_Tenants_TrialSeatsUsed DEFAULT 1,
        TrialStatus        NVARCHAR(32)     NULL,
        TrialSampleRunId   UNIQUEIDENTIFIER NULL;
END;
GO
