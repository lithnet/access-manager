USE [AccessManager]
GO
/****** Object:  Table [dbo].[Authorities]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Authorities](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [AuthorityId] [varchar](256) NOT NULL,
    [AuthorityType] [int] NOT NULL,
 CONSTRAINT [PK_Authorities] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Devices]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Devices](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [ObjectID] [varchar](38) NOT NULL,
    [AgentVersion] [varchar](50) NULL,
    [ComputerName] [nvarchar](64) NOT NULL,
    [DnsName] [nvarchar](256) NULL,
    [Created] [datetime2](7) NOT NULL,
    [Modified] [datetime2](7) NULL,
    [AuthorityKey] [bigint] NOT NULL,
    [AuthorityDeviceId] [varchar](256) NOT NULL,
    [Sid] [varchar](256) NOT NULL,
    [ApprovalState] [int] NULL,
    [OperatingSystemFamily] [nvarchar](50) NULL,
    [OperatingSystemVersion] [nvarchar](50) NULL,
    [Disabled] [bit] NOT NULL,
 CONSTRAINT [PK_Devices] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  View [dbo].[v_Devices]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE VIEW [dbo].[v_Devices]
AS
SELECT dbo.Devices.Id, dbo.Devices.ObjectID, dbo.Authorities.AuthorityType, dbo.Authorities.AuthorityId, dbo.Devices.AuthorityDeviceId, dbo.Devices.AgentVersion, dbo.Devices.ComputerName, dbo.Devices.DnsName, dbo.Devices.Created, 
                  dbo.Devices.Modified, dbo.Devices.Sid, dbo.Devices.ApprovalState, dbo.Devices.OperatingSystemFamily, dbo.Devices.OperatingSystemVersion, dbo.Devices.Disabled
FROM     dbo.Devices INNER JOIN
                  dbo.Authorities ON dbo.Devices.AuthorityKey = dbo.Authorities.Id
GO
/****** Object:  Table [dbo].[DeviceCredentials]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DeviceCredentials](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [DeviceKey] [bigint] NOT NULL,
    [X509Cert] [varbinary](max) NOT NULL,
    [X509CertSha256TP] [nchar](64) NOT NULL,
 CONSTRAINT [PK_DeviceCredentials] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[DevicePasswords]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[DevicePasswords](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [DeviceKey] [bigint] NOT NULL,
    [PasswordData] [nvarchar](max) NOT NULL,
    [EffectiveDate] [datetime2](7) NOT NULL,
    [RetiredDate] [datetime2](7) NULL,
    [RequestId] [nvarchar](38) NOT NULL,
    [AccountName] [nvarchar](50) NOT NULL,
    [ExpiryDate] [datetime2](7) NULL,
 CONSTRAINT [PK_DevicePasswords] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
/****** Object:  Table [dbo].[Groups]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[Groups](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [Name] [nvarchar](256) NOT NULL,
    [Sid] [varchar](256) NOT NULL,
    [Description] [nvarchar](256) NULL,
 CONSTRAINT [PK_Groups] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[MembershipDevices]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[MembershipDevices](
    [Id] [bigint] IDENTITY(1,1) NOT NULL,
    [GroupKey] [bigint] NOT NULL,
    [MemberKey] [bigint] NOT NULL,
 CONSTRAINT [PK_MembershipDevices] PRIMARY KEY CLUSTERED 
(
    [Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
/****** Object:  Table [dbo].[RegistrationKeys]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE TABLE [dbo].[RegistrationKeys](
    [ID] [bigint] IDENTITY(1,1) NOT NULL,
    [RegistrationKey] [varchar](50) NOT NULL,
    [ActivationLimit] [int] NOT NULL,
    [ActivationCount] [int] NOT NULL,
    [Enabled] [bit] NOT NULL,
    [RegistrationKeyName] [nvarchar](100) NULL,
    [ApprovalRequired] [bit] NOT NULL,
 CONSTRAINT [PK_RegistrationKeys] PRIMARY KEY CLUSTERED 
(
    [ID] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Devices] ADD  CONSTRAINT [DF_Devices_Disabled]  DEFAULT ((0)) FOR [Disabled]
GO
ALTER TABLE [dbo].[RegistrationKeys] ADD  CONSTRAINT [DF_Table_1_Activations]  DEFAULT ((0)) FOR [ActivationCount]
GO
ALTER TABLE [dbo].[RegistrationKeys] ADD  CONSTRAINT [DF_RegistrationKeys_ApprovalRequired]  DEFAULT ((0)) FOR [ApprovalRequired]
GO
ALTER TABLE [dbo].[DeviceCredentials]  WITH CHECK ADD  CONSTRAINT [FK_DeviceCredentials_Devices] FOREIGN KEY([DeviceKey])
REFERENCES [dbo].[Devices] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DeviceCredentials] CHECK CONSTRAINT [FK_DeviceCredentials_Devices]
GO
ALTER TABLE [dbo].[DevicePasswords]  WITH CHECK ADD  CONSTRAINT [FK_DevicePasswords_Devices] FOREIGN KEY([DeviceKey])
REFERENCES [dbo].[Devices] ([Id])
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[DevicePasswords] CHECK CONSTRAINT [FK_DevicePasswords_Devices]
GO
ALTER TABLE [dbo].[Devices]  WITH CHECK ADD  CONSTRAINT [FK_Devices_Authorities] FOREIGN KEY([AuthorityKey])
REFERENCES [dbo].[Authorities] ([Id])
GO
ALTER TABLE [dbo].[Devices] CHECK CONSTRAINT [FK_Devices_Authorities]
GO
ALTER TABLE [dbo].[MembershipDevices]  WITH CHECK ADD  CONSTRAINT [FK_MembershipDevices_Groups] FOREIGN KEY([GroupKey])
REFERENCES [dbo].[Groups] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MembershipDevices] CHECK CONSTRAINT [FK_MembershipDevices_Groups]
GO
ALTER TABLE [dbo].[MembershipDevices]  WITH CHECK ADD  CONSTRAINT [FK_MembershipDevices_Groups1] FOREIGN KEY([MemberKey])
REFERENCES [dbo].[Devices] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO
ALTER TABLE [dbo].[MembershipDevices] CHECK CONSTRAINT [FK_MembershipDevices_Groups1]
GO
/****** Object:  StoredProcedure [dbo].[spAddDeviceCredentials]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spAddDeviceCredentials]
    @ID bigint,
    @X509Certificate varbinary(MAX),
    @X509CertificateThumbprint varchar(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

    IF NOT EXISTS (	
            SELECT TOP 1 ID 
            FROM
                DeviceCredentials 
            WHERE 
                DeviceKey = @ID
            AND	
                X509CertSha256TP = @X509CertificateThumbprint)
    BEGIN
        INSERT INTO DeviceCredentials
            (DeviceKey, X509Cert, X509CertSha256TP)
        VALUES
            (@ID, @X509Certificate, @X509CertificateThumbprint)
    END

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spAddDeviceToGroup]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spAddDeviceToGroup]
    @GroupId bigint,
    @DeviceId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION
    
       IF NOT EXISTS (SELECT 1 FROM 
                            dbo.MembershipDevices
                      WHERE  
                            @GroupId = GroupKey 
                     AND
                            @DeviceId = MemberKey)
       BEGIN
           INSERT INTO 
            dbo.MembershipDevices 
                (GroupKey, MemberKey)
           VALUES 
                (@GroupId, @DeviceId)
       END

   COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spApproveDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spApproveDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE  
        dbo.Devices 
    SET 
        ApprovalState = 1
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spConsumeRegistrationKey]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spConsumeRegistrationKey]
    @RegistrationKey varchar(50)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION

        DECLARE @enabled bit;
        DECLARE @id bigint;
        DECLARE @activationLimit int;
        DECLARE @activationCount int;

        SELECT TOP 1
            @enabled = [Enabled],
            @id = [Id],
            @activationLimit = [ActivationLimit],
            @activationCount = [ActivationCount]
        FROM dbo.RegistrationKeys WITH (UPDLOCK)
        WHERE 
            RegistrationKey = @RegistrationKey

        if (@id is null)
            THROW 50003, N'The registration key was not found', 1
        
        IF (@id = 0)
            THROW 50003, N'The registration key was not found', 1
        
        IF (@enabled = 0)
            THROW 50005, N'The registration key is disabled', 1
        
        IF (@activationLimit > 0 AND @activationCount >= @activationLimit)
            THROW 50004, N'The activation limit for the specified key has been exceeded', 1

        UPDATE [dbo].[RegistrationKeys]
        SET 
            ActivationCount = @activationCount + 1
        WHERE
            [ID] = @id
    COMMIT

SELECT TOP 1 *
FROM
    [dbo].[RegistrationKeys]
WHERE 
    ID = @id

END
GO
/****** Object:  StoredProcedure [dbo].[spCreateAuthority]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spCreateAuthority]
    @AuthorityType int,
    @AuthorityId varchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        INSERT INTO Authorities
        (AuthorityId, AuthorityType)
        VALUES
        (@AuthorityId, @AuthorityType)

    COMMIT

    SELECT TOP 1 Id FROM Authorities
    WHERE AuthorityType = @AuthorityType and AuthorityId = @AuthorityId
END
GO
/****** Object:  StoredProcedure [dbo].[spCreateDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spCreateDevice]
    @ObjectID varchar(38),
    @AgentVersion  varchar(50) = null,
    @ComputerName nvarchar(64),
    @DnsName nvarchar(256) = null,
    @AuthorityKey bigint,
    @AuthorityDeviceId varchar(256),
    @ApprovalState int,
    @SID varchar(256),
    @OSFamily nvarchar(50) = null,
    @OSVersion nvarchar(50) = null
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        INSERT INTO Devices
        (ObjectID, AgentVersion, ComputerName, DnsName, AuthorityKey, AuthorityDeviceId, ApprovalState, [SID], OperatingSystemFamily, OperatingSystemVersion, Created, Modified)
        VALUES
        (@ObjectID, @AgentVersion, @ComputerName, @DnsName, @AuthorityKey, @AuthorityDeviceId, @ApprovalState, @SID, @OSFamily, @OSVersion, SYSUTCDATETIME(), SYSUTCDATETIME())

    COMMIT
    SELECT TOP 1 * FROM v_Devices
    Where ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spCreateDeviceWithCredentials]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spCreateDeviceWithCredentials]
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
    @X509Certificate varbinary(MAX),
    @X509CertificateThumbprint varchar(64)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION
    
        INSERT INTO Devices
        (ObjectID, AgentVersion, ComputerName, DnsName, AuthorityKey, AuthorityDeviceId, ApprovalState, [SID], OperatingSystemFamily, OperatingSystemVersion, Created, Modified)
        VALUES
        (@ObjectID, @AgentVersion, @ComputerName, @DnsName, @AuthorityKey, @AuthorityDeviceId, @ApprovalState, @SID, @OSFamily, @OSVersion, SYSUTCDATETIME(), SYSUTCDATETIME())

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
/****** Object:  StoredProcedure [dbo].[spCreateGroup]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spCreateGroup]
    @Name nvarchar(256),
    @Sid varchar(256),
    @Description nvarchar(256) = null
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        INSERT INTO [dbo].[Groups]
        ([Name], [Sid], [Description])
        VALUES
        (@Name, @sid, @Description)

        DECLARE @key bigint = (SELECT SCOPE_IDENTITY());
        
        SELECT TOP 1 * from dbo.Groups
        WHERE ID = @key
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spCreateRegistrationKey]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spCreateRegistrationKey]
    @RegistrationKey varchar(50),
    @RegistrationKeyName nvarchar(100),
    @ActivationLimit int,
    @ActivationCount int,
    @ApprovalRequired bit,
    @Enabled bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        INSERT INTO [dbo].[RegistrationKeys]
        (RegistrationKey, RegistrationKeyName, ActivationLimit, ActivationCount, [Enabled], ApprovalRequired)
        VALUES
        (@RegistrationKey, @RegistrationKeyName, @ActivationLimit, @ActivationCount, @Enabled, @ApprovalRequired)

        DECLARE @key bigint = (SELECT SCOPE_IDENTITY());
        
        SELECT TOP 1 * from dbo.RegistrationKeys
        WHERE ID = @key
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spDeleteDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spDeleteDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    DELETE FROM 
        dbo.Devices 
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spDeleteGroup]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spDeleteGroup]
    @ID bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        DELETE FROM [dbo].[Groups]
        WHERE
        ID = @ID
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spDeleteRegistrationKey]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spDeleteRegistrationKey]
    @ID bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        DELETE FROM [dbo].[RegistrationKeys]
        WHERE
        ID = @ID
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spDisableDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spDisableDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE  
        dbo.Devices 
    SET 
        [Disabled] = 1
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spEnableDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spEnableDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE  
        dbo.Devices 
    SET 
        [Disabled] = 0
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spExpireCurrentPassword]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spExpireCurrentPassword]
    @ObjectId varchar(38)
AS
BEGIN
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    SET XACT_ABORT ON;
    BEGIN TRANSACTION
        if (@DeviceKey IS NULL)
            THROW 50000, N'The specified device ID was not found', 1

        UPDATE DevicePasswords
        SET RetiredDate = GETUTCDATE()
        WHERE Id = (SELECT Id 
                    FROM DevicePasswords
                    WHERE DeviceKey = @DeviceKey
                    AND RetiredDate is NULL)
    
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spGetCurrentPassword]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetCurrentPassword]
    @ObjectId varchar(38)
AS
BEGIN
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    if (@DeviceKey IS NULL)
        THROW 50000, N'The specified device ID was not found', 1

SELECT TOP 1 *
    FROM 
        DevicePasswords
    WHERE
        DeviceKey = @DeviceKey
    AND
        RetiredDate is  NULL
    ORDER BY 
        EffectiveDate DESC
    
END
GO
/****** Object:  StoredProcedure [dbo].[spGetCurrentPasswordAndUpdateExpiry]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetCurrentPasswordAndUpdateExpiry]
    @ObjectId varchar(38),
    @ExpiryDate datetime2(7)
AS
BEGIN
    SET XACT_ABORT ON;
    BEGIN TRANSACTION
        DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    if (@DeviceKey IS NULL)
        THROW 50000, N'The specified device ID was not found', 1
     
     DECLARE @PasswordID varchar(38) = (
         SELECT TOP 1 RequestId
            FROM 
                DevicePasswords
            WHERE
                DeviceKey = @DeviceKey
            AND
                RetiredDate is NULL
            ORDER BY 
                EffectiveDate DESC
            )
    
    if (@PasswordID is NULL) RETURN;
    
    UPDATE DevicePasswords
    SET ExpiryDate = @ExpiryDate
    WHERE
    RequestId = @PasswordID

    SELECT TOP 1 *
    FROM DevicePasswords
    WHERE RequestId = @PasswordID

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spGetDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 * FROM 
        v_Devices 
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDeviceByAuthority]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetDeviceByAuthority]
    @AuthorityType int,
    @AuthorityId varchar(256),
    @AuthorityDeviceId varchar(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        v_Devices 
    WHERE
        AuthorityId = @AuthorityId AND
        AuthorityType = @AuthorityType AND
        AuthorityDeviceId = @AuthorityDeviceId
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDeviceByX509Thumbprint]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[spGetDeviceByX509Thumbprint]
    @Thumbprint varchar(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 devices.* 
        FROM 
            dbo.v_Devices devices
        INNER JOIN dbo.DeviceCredentials c
            ON devices.Id = c.DeviceKey
        WHERE 
            c.X509CertSha256TP = @Thumbprint
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDeviceGroupMembership]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spGetDeviceGroupMembership]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT g.*
    FROM
        [dbo].[Groups] g
    INNER JOIN
        [dbo].MembershipDevices m
    ON 
        m.GroupKey = g.Id
    INNER JOIN
        [dbo].[Devices] d
    ON 
        d.id = m.MemberKey
    WHERE
        d.ObjectID = @ObjectID
        
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDevicesByComputerName]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetDevicesByComputerName]
    @ComputerName nvarchar(64)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        v_Devices 
    WHERE
        ComputerName = @ComputerName
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDevicesByNames]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetDevicesByNames]
    @ComputerNameOrDnsName nvarchar(256)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        v_Devices 
    WHERE
        ComputerName = @ComputerNameOrDnsName
    OR
        DnsName = @ComputerNameOrDnsName
END
GO
/****** Object:  StoredProcedure [dbo].[spGetDevicesByPage]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetDevicesByPage]
    @startIndex int,
    @rows int
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        v_Devices 
    ORDER BY Id
    OFFSET @startIndex ROWS FETCH NEXT @rows ROWS ONLY;
END
GO
/****** Object:  StoredProcedure [dbo].[spGetGroupDeviceMembers]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetGroupDeviceMembers]
    @GroupId bigint
AS
BEGIN
    SET NOCOUNT ON;

    SELECT d.* FROM 
        dbo.v_Devices d
    INNER JOIN
        dbo.MembershipDevices m
    ON	
        d.Id = m.MemberKey
    WHERE 
        m.GroupKey = @GroupId

END
GO
/****** Object:  StoredProcedure [dbo].[spGetGroups]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetGroups]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        dbo.Groups 
END
GO
/****** Object:  StoredProcedure [dbo].[spGetOrCreateAuthority]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetOrCreateAuthority]
    @AuthorityType int,
    @AuthorityId varchar(256)
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @Key bigint = (SELECT TOP 1 Id FROM Authorities WHERE AuthorityType = @AuthorityType and AuthorityId = @AuthorityId)

    IF (@KEY IS NOT NULL)
        BEGIN
            SELECT @Key
        END
    ELSE
        BEGIN
            EXEC [dbo].[spCreateAuthority] @AuthorityType, @AuthorityId
        END
END
GO
/****** Object:  StoredProcedure [dbo].[spGetPasswordHistory]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetPasswordHistory]
    @ObjectId varchar(38)
AS
BEGIN
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    if (@DeviceKey IS NULL)
        THROW 50000, N'The specified device ID was not found', 1

SELECT *
    FROM 
        DevicePasswords
    WHERE
        DeviceKey = @DeviceKey
    AND
        RetiredDate is NOT NULL
    ORDER BY 
        EffectiveDate DESC
    
END
GO
/****** Object:  StoredProcedure [dbo].[spGetRegistrationKey]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetRegistrationKey]	
    @RegistrationKey varchar(50)
AS
BEGIN
    SET NOCOUNT ON;

    SELECT TOP 1 * FROM 
        dbo.RegistrationKeys 
    WHERE 
        [RegistrationKey] = @RegistrationKey
END
GO
/****** Object:  StoredProcedure [dbo].[spGetRegistrationKeys]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetRegistrationKeys]
AS
BEGIN
    SET NOCOUNT ON;

    SELECT * FROM 
        dbo.RegistrationKeys 
END
GO
/****** Object:  StoredProcedure [dbo].[spHasPasswordExpired]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
CREATE PROCEDURE [dbo].[spHasPasswordExpired]
    @ObjectID varchar(38),
    @RotationDate datetime2(7)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    if (@DeviceKey IS NULL)
        THROW 50000, N'The specified device ID was not found', 1

    DECLARE @EffectiveDate datetime2(7) = 
    (
        SELECT TOP 1 EffectiveDate FROM 
            DevicePasswords
        WHERE 
            DeviceKey = @DeviceKey
        AND		
            RetiredDate is NULL
        ORDER BY 
            EffectiveDate DESC
    )

    IF (@EffectiveDate is NULL)
        RETURN 1;

    IF (@EffectiveDate < @RotationDate)
        RETURN 1;

    RETURN 0;
END
GO
/****** Object:  StoredProcedure [dbo].[spPurgePasswordHistory]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spPurgePasswordHistory]
    @ObjectID varchar(38),
    @MinEntries int,
    @PurgeBefore datetime2(7)
AS
BEGIN
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        IF (@DeviceKey IS NULL)
            THROW 50000, N'The specified device ID was not found', 1

        SELECT * FROM 
            DevicePasswords
        WHERE 
            Id NOT IN 
                (SELECT TOP (@MinEntries) Id FROM 
                    DevicePasswords
                WHERE
                    RetiredDate IS NOT NULL
                AND
                    DeviceKey = @DeviceKey
                ORDER BY 
                    RetiredDate DESC)
            AND RetiredDate IS NOT NULL
            AND RetiredDate < @PurgeBefore 
            AND DeviceKey = @DeviceKey
    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spRejectDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spRejectDevice]
    @ObjectID varchar(38)
AS
BEGIN
    SET NOCOUNT ON;

    UPDATE  
        dbo.Devices 
    SET 
        ApprovalState = 2
    WHERE
        ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spRemoveDeviceFromGroup]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spRemoveDeviceFromGroup]
    @GroupId bigint,
    @DeviceId bigint
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION
    
    DELETE FROM 
        dbo.MembershipDevices
    WHERE  
        @GroupId = GroupKey 
    AND
        @DeviceId = MemberKey

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spRollbackPasswordUpdate]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spRollbackPasswordUpdate]
    @ObjectID varchar(38),
    @RequestId varchar(38)
AS
BEGIN
    SET NOCOUNT ON;
    
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        if (@DeviceKey IS NULL)
            THROW 50000, N'The specified device ID was not found', 1

        DELETE FROM 
            DevicePasswords
        WHERE 
            DeviceKey = @DeviceKey
        AND		
            RequestId = @RequestId;

        IF @@ROWCOUNT = 1
            BEGIN
                UPDATE DevicePasswords
                SET RetiredDate = NULL
                WHERE Id = (SELECT TOP 1 Id 
                            FROM DevicePasswords
                            WHERE @DeviceKey = @DeviceKey
                            ORDER BY EffectiveDate DESC)
            END

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spUpdateCurrentPassword]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spUpdateCurrentPassword]
    @ObjectId varchar(38),
    @PasswordData nvarchar(max),
    @RequestId varchar(38),
    @AccountName nvarchar(50),
    @EffectiveDate datetime2(7),
    @ExpiryDate datetime2(7)
AS
BEGIN
    DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

    SET XACT_ABORT ON;
    BEGIN TRANSACTION
        if (@DeviceKey IS NULL)
            THROW 50000, N'The specified device ID was not found', 1

        UPDATE DevicePasswords
        SET RetiredDate = @EffectiveDate
        WHERE Id = (SELECT Id 
                    FROM DevicePasswords
                    WHERE DeviceKey = @DeviceKey
                    AND RetiredDate is NULL)
    
        INSERT INTO DevicePasswords
        (DeviceKey, PasswordData, EffectiveDate, RequestId, AccountName, ExpiryDate) 
        VALUES (@DeviceKey, @PasswordData, @EffectiveDate, @RequestId, @AccountName, @ExpiryDate)

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spUpdateDevice]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spUpdateDevice]
    @ObjectID varchar(38),
    @AgentVersion  varchar(50),
    @ComputerName nvarchar(64),
    @DnsName nvarchar(256) = null,
    @OSFamily nvarchar(50) = null,
    @OSVersion nvarchar(50) = null
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
            Modified = SYSUTCDATETIME()		
        WHERE ObjectID = @ObjectID

    COMMIT
    SELECT TOP 1 * FROM v_Devices
    Where ObjectID = @ObjectID
END
GO
/****** Object:  StoredProcedure [dbo].[spUpdateGroup]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spUpdateGroup]
    @ID bigint,
    @Name nvarchar(256),
    @Description nvarchar(256) = null
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        UPDATE [dbo].[Groups]
        SET
            [Name] = @Name,
            [Description]= @Description
        WHERE
        ID = @ID
        
        SELECT TOP 1 * FROM dbo.Groups
        WHERE ID = @ID

    COMMIT
END
GO
/****** Object:  StoredProcedure [dbo].[spUpdateRegistrationKey]    Script Date: 11/07/2021 8:16:47 AM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spUpdateRegistrationKey]
    @ID bigint,
    @RegistrationKey varchar(50),
    @RegistrationKeyName nvarchar(100),
    @ActivationLimit int,
    @ActivationCount int,
    @ApprovalRequired bit,
    @Enabled bit
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;
    BEGIN TRANSACTION

        UPDATE [dbo].[RegistrationKeys]
        SET
            RegistrationKey = @RegistrationKey,
            RegistrationKeyName = @RegistrationKeyName,
            ActivationLimit = @ActivationLimit,
            ActivationCount = @ActivationCount,
            ApprovalRequired =@ApprovalRequired,
            [Enabled] = @Enabled
        WHERE
        ID = @ID
        
        SELECT TOP 1 * FROM dbo.RegistrationKeys
        WHERE ID = @ID

    COMMIT
END
GO