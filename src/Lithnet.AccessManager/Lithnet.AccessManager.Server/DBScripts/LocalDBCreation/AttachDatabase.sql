USE [master]

CREATE DATABASE [AccessManager]
    ON (FILENAME = N'{localDbPath}')
    FOR ATTACH;