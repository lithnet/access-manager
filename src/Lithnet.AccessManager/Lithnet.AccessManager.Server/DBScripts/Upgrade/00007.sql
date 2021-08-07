USE [AccessManager]
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
