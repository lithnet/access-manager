USE [master]

IF DB_ID('AccessManager') IS NULL 
BEGIN
    CREATE DATABASE [AccessManager]
END

GO

USE [master]

IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = '{serviceAccount}')
BEGIN
    CREATE LOGIN [{serviceAccount}] FROM WINDOWS WITH DEFAULT_DATABASE=[AccessManager]
END

GO

USE [AccessManager]

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = '{serviceAccount}')
BEGIN
    CREATE USER [{serviceAccount}] FOR LOGIN [{serviceAccount}] 
END

GO

USE [AccessManager]
ALTER ROLE [db_owner] ADD MEMBER [{serviceAccount}]

GO

