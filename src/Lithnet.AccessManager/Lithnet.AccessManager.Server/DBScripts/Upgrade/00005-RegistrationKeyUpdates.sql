USE [AccessManager]
GO

BEGIN TRANSACTION
GO
ALTER TABLE dbo.Groups ADD
	Type int NOT NULL CONSTRAINT DF_Groups_Type DEFAULT 0
GO
ALTER TABLE dbo.Groups SET (LOCK_ESCALATION = TABLE)
GO
COMMIT
GO


ALTER PROCEDURE [dbo].[spDeleteGroup]
	@ID bigint
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	BEGIN TRANSACTION
		
		DECLARE @Type int;

		SELECT @Type=[Type] FROM
			[dbo].[Groups]
		WHERE
			ID = @ID

		IF (@Type = 1)
			THROW 50006, N'Cannot delete a system group', 1

		DELETE FROM 
			[dbo].[Groups]
		WHERE
			ID = @ID
	COMMIT
END

GO

CREATE TABLE [dbo].[RegistrationKeyGroupMapping](
	[Id] [bigint] IDENTITY(1,1) NOT NULL,
	[RegistrationKeyKey] [bigint] NOT NULL,
	[GroupKey] [bigint] NOT NULL,
 CONSTRAINT [PK_RegistrationKeyGroupMapping] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[RegistrationKeyGroupMapping]  WITH CHECK ADD  CONSTRAINT [FK_RegistrationKeyGroupMapping_RegistrationKeys] FOREIGN KEY([RegistrationKeyKey])
REFERENCES [dbo].[RegistrationKeys] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RegistrationKeyGroupMapping] CHECK CONSTRAINT [FK_RegistrationKeyGroupMapping_RegistrationKeys]
GO

ALTER TABLE [dbo].[RegistrationKeyGroupMapping]  WITH CHECK ADD  CONSTRAINT [FK_RegistrationKeyGroupMapping_Groups] FOREIGN KEY([GroupKey])
REFERENCES [dbo].[Groups] ([Id])
ON UPDATE CASCADE
ON DELETE CASCADE
GO

ALTER TABLE [dbo].[RegistrationKeyGroupMapping] CHECK CONSTRAINT [FK_RegistrationKeyGroupMapping_Groups]
GO

CREATE UNIQUE NONCLUSTERED INDEX [IX_RegistrationKeyGroupMapping] ON [dbo].[RegistrationKeyGroupMapping]
(
	[RegistrationKeyKey] ASC,
	[GroupKey] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, SORT_IN_TEMPDB = OFF, IGNORE_DUP_KEY = OFF, DROP_EXISTING = OFF, ONLINE = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
GO


CREATE PROCEDURE [dbo].[spRemoveGroupFromRegistrationKey]
	@GroupId bigint,
	@RegistrationKeyId bigint
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	BEGIN TRANSACTION
	
	DELETE FROM 
		dbo.RegistrationKeyGroupMapping
	WHERE  
		@GroupId = GroupKey 
	AND
		@RegistrationKeyId = RegistrationKeyKey

	COMMIT
END

GO

CREATE PROCEDURE [dbo].[spAddGroupToRegistrationKey]
	@GroupId bigint,
	@RegistrationKeyId bigint
AS
BEGIN
	SET NOCOUNT ON;
	SET XACT_ABORT ON;
	BEGIN TRANSACTION
	
	   IF NOT EXISTS (SELECT 1 FROM 
							dbo.RegistrationKeyGroupMapping
					  WHERE  
				 			@GroupId = GroupKey 
					 AND
							@RegistrationKeyId = RegistrationKeyKey)
	   BEGIN
		   INSERT INTO 
			dbo.RegistrationKeyGroupMapping 
				(GroupKey, RegistrationKeyKey)
		   VALUES 
				(@GroupId, @RegistrationKeyId)
	   END

   COMMIT
END

GO


CREATE PROCEDURE [dbo].[spGetRegistrationKeyGroups]
	@ID bigint
AS
BEGIN
	SET NOCOUNT ON;

	SELECT g.*
	FROM
		[dbo].[Groups] g
	INNER JOIN
		[dbo].[RegistrationKeyGroupMapping] m
	ON 
		m.[GroupKey] = g.[Id]
	WHERE
		m.[RegistrationKeyKey] = @ID
		
END
GO


ALTER PROCEDURE [dbo].[spExpireCurrentPassword]
	@ObjectId varchar(38)
AS
BEGIN
	DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

	SET XACT_ABORT ON;
	BEGIN TRANSACTION
		if (@DeviceKey IS NULL)
			THROW 50000, N'The specified device ID was not found', 1

		UPDATE DevicePasswords
		SET ExpiryDate = GETUTCDATE()
		WHERE Id = (SELECT Id 
					FROM DevicePasswords
					WHERE DeviceKey = @DeviceKey
					AND RetiredDate is NULL)
	
	COMMIT
END
GO

DROP PROCEDURE [dbo].[spHasPasswordExpired]
GO
