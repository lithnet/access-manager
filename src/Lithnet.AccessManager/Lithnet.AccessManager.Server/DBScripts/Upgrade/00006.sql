USE [AccessManager]
GO
BEGIN TRANSACTION
SET QUOTED_IDENTIFIER ON
SET ARITHABORT ON
SET NUMERIC_ROUNDABORT OFF
SET CONCAT_NULL_YIELDS_NULL ON
SET ANSI_NULLS ON
SET ANSI_PADDING ON
SET ANSI_WARNINGS ON
COMMIT
BEGIN TRANSACTION
GO
ALTER TABLE dbo.Devices ADD
    OperatingSystemType int NOT NULL CONSTRAINT DF_Devices_OperatingSystemType DEFAULT 0
GO
ALTER TABLE dbo.Devices SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
GO


ALTER PROCEDURE [dbo].[spCreateDevice]
    @ObjectID varchar(38),
    @AgentVersion  varchar(50) = null,
    @ComputerName nvarchar(64),
    @DnsName nvarchar(256) = null,
    @AuthorityKey bigint,
    @AuthorityDeviceId varchar(256),
    @ApprovalState int,
    @SID varchar(256),
    @OSFamily nvarchar(50) = null,
    @OSVersion nvarchar(50) = null,
    @OSType int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        INSERT INTO Devices
        (ObjectID, AgentVersion, ComputerName, DnsName, AuthorityKey, AuthorityDeviceId, ApprovalState, [SID], OperatingSystemFamily, OperatingSystemVersion, OperatingSystemType, Created, Modified)
        VALUES
        (@ObjectID, @AgentVersion, @ComputerName, @DnsName, @AuthorityKey, @AuthorityDeviceId, @ApprovalState, @SID, @OSFamily, @OSVersion, @OSType, SYSUTCDATETIME(), SYSUTCDATETIME())

    COMMIT
    SELECT TOP 1 * FROM v_Devices
    Where ObjectID = @ObjectID
END

GO



ALTER PROCEDURE [dbo].[spCreateDeviceWithCredentials]
    @ObjectID varchar(38),
    @AgentVersion  varchar(50) = null,
    @ComputerName nvarchar(64),
    @DnsName nvarchar(256) = null,
    @AuthorityKey bigint,
    @AuthorityDeviceId varchar(256),
    @ApprovalState int,
    @SID varchar(256),
    @OSFamily nvarchar(50) = null,
    @OSVersion nvarchar(50) = null,
    @OSType int,
    @X509Certificate varbinary(MAX),
    @X509CertificateThumbprint varchar(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION
    
        INSERT INTO Devices
        (ObjectID, AgentVersion, ComputerName, DnsName, AuthorityKey, AuthorityDeviceId, ApprovalState, [SID], OperatingSystemFamily, OperatingSystemVersion, OperatingSystemType, Created, Modified)
        VALUES
        (@ObjectID, @AgentVersion, @ComputerName, @DnsName, @AuthorityKey, @AuthorityDeviceId, @ApprovalState, @SID, @OSFamily, @OSVersion, @OSType, SYSUTCDATETIME(), SYSUTCDATETIME())

        DECLARE @key bigint = (SELECT SCOPE_IDENTITY());

        INSERT INTO DeviceCredentials
        (DeviceKey, X509Cert, X509CertSha256TP)
        VALUES
        (@key, @X509Certificate, @X509CertificateThumbprint)

    COMMIT

    SELECT TOP 1 * FROM v_Devices
    Where ObjectID = @ObjectID
END

GO



ALTER PROCEDURE [dbo].[spUpdateDevice]
    @ObjectID varchar(38),
    @AgentVersion  varchar(50),
    @ComputerName nvarchar(64),
    @DnsName nvarchar(256) = null,
    @OSFamily nvarchar(50) = null,
    @OSVersion nvarchar(50) = null,
    @OSType int
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        UPDATE Devices
        SET 
            AgentVersion = @AgentVersion,
            ComputerName = @ComputerName,
            DnsName = @DnsName,
            OperatingSystemFamily = @OSFamily,
            OperatingSystemVersion = @OSVersion,
            OperatingSystemType = @OSType,
            Modified = SYSUTCDATETIME()		
        WHERE ObjectID = @ObjectID

    COMMIT
    SELECT TOP 1 * FROM v_Devices
    Where ObjectID = @ObjectID
END

GO



ALTER VIEW [dbo].[v_Devices]
AS
SELECT dbo.Devices.Id, dbo.Devices.ObjectID, dbo.Authorities.AuthorityType, dbo.Authorities.AuthorityId, dbo.Devices.AuthorityDeviceId, dbo.Devices.AgentVersion, dbo.Devices.ComputerName, dbo.Devices.DnsName, dbo.Devices.Created, 
                  dbo.Devices.Modified, dbo.Devices.Sid, dbo.Devices.ApprovalState, dbo.Devices.OperatingSystemFamily, dbo.Devices.OperatingSystemVersion, dbo.Devices.OperatingSystemType, dbo.Devices.Disabled
FROM     dbo.Devices INNER JOIN
                  dbo.Authorities ON dbo.Devices.AuthorityKey = dbo.Authorities.Id
GO



