USE [master]
GO
IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE LOGIN [NT SERVICE\lithnetams] FROM WINDOWS WITH DEFAULT_DATABASE=[AccessManager]
END

GO
USE [AccessManager]
GO


IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE USER [NT SERVICE\lithnetams] FOR LOGIN [NT SERVICE\lithnetams]
END

GO
USE [AccessManager]
GO
ALTER ROLE [db_owner] ADD MEMBER [NT SERVICE\lithnetams]
GO
