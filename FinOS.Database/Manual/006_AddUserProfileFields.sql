-- Adds optional profile fields introduced for Settings > Profile Information.
-- Safe to run repeatedly on an existing FinOS database.
USE FinOS;
GO

IF COL_LENGTH(N'Security.Users', N'DateOfBirth') IS NULL
    ALTER TABLE Security.Users ADD DateOfBirth DATE NULL;
IF COL_LENGTH(N'Security.Users', N'Bio') IS NULL
    ALTER TABLE Security.Users ADD Bio NVARCHAR(2000) NULL;
GO