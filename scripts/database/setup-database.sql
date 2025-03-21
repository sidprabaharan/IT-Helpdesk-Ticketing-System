-- Create database if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'HelpdeskTicketing')
BEGIN
    CREATE DATABASE HelpdeskTicketing;
END
GO

USE HelpdeskTicketing;
GO

-- Create schema if it doesn't exist
IF NOT EXISTS (SELECT * FROM sys.schemas WHERE name = 'helpdesk')
BEGIN
    EXEC('CREATE SCHEMA helpdesk');
END
GO

-- Create tables
-- AspNetUsers (extended from ASP.NET Identity)
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AspNetUsers')
BEGIN
    CREATE TABLE [dbo].[AspNetUsers](
        [Id] NVARCHAR(450) NOT NULL,
        [UserName] NVARCHAR(256) NULL,
        [NormalizedUserName] NVARCHAR(256) NULL,
        [Email] NVARCHAR(256) NULL,
        [NormalizedEmail] NVARCHAR(256) NULL,
        [EmailConfirmed] BIT NOT NULL,
        [PasswordHash] NVARCHAR(MAX) NULL,
        [SecurityStamp] NVARCHAR(MAX) NULL,
        [ConcurrencyStamp] NVARCHAR(MAX) NULL,
        [PhoneNumber] NVARCHAR(MAX) NULL,
        [PhoneNumberConfirmed] BIT NOT NULL,
        [TwoFactorEnabled] BIT NOT NULL,
        [LockoutEnd] DATETIMEOFFSET NULL,
        [LockoutEnabled] BIT NOT NULL,
        [AccessFailedCount] INT NOT NULL,
        [FirstName] NVARCHAR(100) NULL,
        [LastName] NVARCHAR(100) NULL,
        [Department] NVARCHAR(100) NULL,
        [JobTitle] NVARCHAR(100) NULL,
        [CreatedAt] DATETIME NOT NULL,
        [LastLoginAt] DATETIME NULL,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LockoutUntil] DATETIME NULL,
        CONSTRAINT [PK_AspNetUsers] PRIMARY KEY ([Id])
    );
END
GO

-- Continue with the rest of the database setup script from the artifact
