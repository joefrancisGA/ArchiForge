SET NOCOUNT ON;
GO

/* R115: Rollback 115_Tenants_StructuredBaseline.sql — remove structured-baseline columns from dbo.Tenants. */

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_ArchitectureTeamSize_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_ArchitectureTeamSize_Positive;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselinePeoplePerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_BaselinePeoplePerReview_Positive;
END;
GO

IF EXISTS (
    SELECT 1
    FROM sys.check_constraints
    WHERE name = N'CK_Tenants_BaselineManualPrepHoursPerReview_Positive'
      AND parent_object_id = OBJECT_ID(N'dbo.Tenants', N'U'))
BEGIN
    ALTER TABLE dbo.Tenants DROP CONSTRAINT CK_Tenants_BaselineManualPrepHoursPerReview_Positive;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'IndustryVerticalOther') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN IndustryVerticalOther;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'IndustryVertical') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN IndustryVertical;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'ArchitectureTeamSize') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN ArchitectureTeamSize;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'CompanySize') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN CompanySize;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'BaselineManualPrepCapturedUtc') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN BaselineManualPrepCapturedUtc;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'BaselinePeoplePerReview') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN BaselinePeoplePerReview;
END;
GO

IF COL_LENGTH(N'dbo.Tenants', N'BaselineManualPrepHoursPerReview') IS NOT NULL
BEGIN
    ALTER TABLE dbo.Tenants DROP COLUMN BaselineManualPrepHoursPerReview;
END;
GO
