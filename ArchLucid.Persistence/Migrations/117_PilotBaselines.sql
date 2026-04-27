SET NOCOUNT ON;
GO

/* 117: In-product pilot scorecard — manual ROI baselines per tenant (Dapper + tenant scope in API). */

IF OBJECT_ID(N'dbo.PilotBaselines', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PilotBaselines
    (
        TenantId UNIQUEIDENTIFIER NOT NULL
            CONSTRAINT PK_PilotBaselines PRIMARY KEY
            CONSTRAINT FK_PilotBaselines_Tenants FOREIGN KEY (TenantId) REFERENCES dbo.Tenants (Id),
        BaselineHoursPerReview     DECIMAL(18, 2) NULL,
        BaselineReviewsPerQuarter  INT              NULL,
        BaselineArchitectHourlyCost DECIMAL(18, 2) NULL,
        UpdatedUtc                 DATETIME2(7)     NOT NULL
            CONSTRAINT DF_PilotBaselines_UpdatedUtc DEFAULT SYSUTCDATETIME()
    );
END;
GO
