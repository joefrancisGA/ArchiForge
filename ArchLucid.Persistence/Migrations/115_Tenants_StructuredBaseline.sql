SET NOCOUNT ON;
GO

/* 115: Structured baseline intake — company profile + deferrable ROI fields (see docs). */

IF COL_LENGTH(N'dbo.Tenants', N'BaselineManualPrepHoursPerReview') IS NULL
BEGIN
    ALTER TABLE dbo.Tenants ADD
        BaselineManualPrepHoursPerReview     DECIMAL(9,2)     NULL,
        BaselinePeoplePerReview              INT              NULL,
        BaselineManualPrepCapturedUtc        DATETIMEOFFSET(7) NULL,
        CompanySize                          NVARCHAR(30)     NULL,
        ArchitectureTeamSize                 INT              NULL,
        IndustryVertical                     NVARCHAR(100)    NULL,
        IndustryVerticalOther                NVARCHAR(200)    NULL;
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselineManualPrepHoursPerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_BaselineManualPrepHoursPerReview_Positive
        CHECK (BaselineManualPrepHoursPerReview IS NULL OR BaselineManualPrepHoursPerReview > 0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselinePeoplePerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_BaselinePeoplePerReview_Positive
        CHECK (BaselinePeoplePerReview IS NULL OR BaselinePeoplePerReview > 0);
END;
GO

IF NOT EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_ArchitectureTeamSize_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants ADD CONSTRAINT CK_Tenants_ArchitectureTeamSize_Positive
        CHECK (ArchitectureTeamSize IS NULL OR ArchitectureTeamSize > 0);
END;
GO
