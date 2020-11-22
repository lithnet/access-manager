USE [master]

CREATE DATABASE [AccessManager]   
    ON (FILENAME = N'{localDbPath}'),   
    (FILENAME = N'{localDbLogPath}')   
    FOR ATTACH;