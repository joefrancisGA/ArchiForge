/* Distributed leader leases for singleton hosted services (advisory scan, archival, retrieval outbox). */
IF OBJECT_ID(N'dbo.HostLeaderLeases', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.HostLeaderLeases
    (
        LeaseName          NVARCHAR(128) NOT NULL CONSTRAINT PK_HostLeaderLeases PRIMARY KEY,
        HolderInstanceId   NVARCHAR(256) NOT NULL,
        LeaseExpiresUtc    DATETIME2     NOT NULL
    );
END;
GO
