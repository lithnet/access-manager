USE [AccessManager]

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = '{serviceAccount}')
BEGIN
    CREATE USER [{serviceAccount}] FOR LOGIN [{serviceAccount}] 
END