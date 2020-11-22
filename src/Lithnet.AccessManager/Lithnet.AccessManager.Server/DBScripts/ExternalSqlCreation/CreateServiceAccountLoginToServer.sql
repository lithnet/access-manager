USE [master]

IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = '{serviceAccount}')
BEGIN
    CREATE LOGIN [{serviceAccount}] FROM WINDOWS WITH DEFAULT_DATABASE=[AccessManager]
END