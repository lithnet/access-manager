USE [master]

IF DB_ID('AccessManager') IS NULL 
BEGIN
    CREATE DATABASE [AccessManager]
END