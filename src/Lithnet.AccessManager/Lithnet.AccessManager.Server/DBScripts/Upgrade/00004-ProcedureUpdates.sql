USE [AccessManager]
GO
/****** Object:  StoredProcedure [dbo].[spGetDevicesByPage]    Script Date: 17/07/2021 7:52:13 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

CREATE PROCEDURE [dbo].[spGetDevices]
AS
BEGIN
	SET NOCOUNT ON;

	SELECT * FROM 
		v_Devices 
END

GO

/****** Object:  StoredProcedure [dbo].[spGetDevice]    Script Date: 17/07/2021 6:22:46 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO


CREATE PROCEDURE [dbo].[spGetGroupBySid]
	@SID varchar(256)
AS
BEGIN
	SET NOCOUNT ON;

	SELECT TOP 1 * FROM 
		Groups 
	WHERE
		[Sid] = @SID
END

GO

USE [AccessManager]
GO
/****** Object:  StoredProcedure [dbo].[spGetCurrentPasswordAndUpdateExpiry]    Script Date: 17/07/2021 8:14:40 PM ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO

ALTER PROCEDURE [dbo].[spGetCurrentPasswordAndUpdateExpiry]
	@ObjectId varchar(38),
	@ExpiryDate datetime2(7)
AS
BEGIN
	SET XACT_ABORT ON;
	BEGIN TRANSACTION
		DECLARE @DeviceKey bigint = (SELECT id from dbo.Devices where ObjectID = @ObjectId)

		IF (@DeviceKey IS NULL)
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
	
		IF (@PasswordID is NULL)
		BEGIN
			ROLLBACK TRANSACTION
			RETURN
		END
	
		UPDATE
			DevicePasswords
		SET 
			ExpiryDate = @ExpiryDate
		WHERE
			RequestId = @PasswordID

		SELECT TOP 1 *
		FROM 
			DevicePasswords
		WHERE
			RequestId = @PasswordID

	COMMIT
END
GO
