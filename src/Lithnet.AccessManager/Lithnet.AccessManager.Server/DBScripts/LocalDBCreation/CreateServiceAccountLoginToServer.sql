USE [master]

IF NOT EXISTS 
    (SELECT name  
     FROM master.sys.server_principals
     WHERE name = 'NT SERVICE\lithnetams')
BEGIN
    CREATE LOGIN [NT SERVICE\lithnetams] FROM WINDOWS WITH DEFAULT_DATABASE=[AccessManager]
END