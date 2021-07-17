USE [AccessManager]

IF NOT EXISTS
    (SELECT name
     FROM sys.database_principals
     WHERE name = '{amsAdminsGroup}')
BEGIN
    CREATE USER [{amsAdminsGroup}] FOR LOGIN [{amsAdminsGroup}] 
END