USE [AccessManager]

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE USER [NT SERVICE\lithnetams] FOR LOGIN [NT SERVICE\lithnetams]
END