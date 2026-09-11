/*
   Incremental schema migration: TravelGroupInteractions

   Purpose:
   Add the interaction table required by the TravelGroups and Favorites
   features without dropping or rebuilding existing data.

   Safe to run repeatedly against LazyTravelDB.
*/
USE [LazyTravelDB];
GO

SET XACT_ABORT ON;
GO

BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.TravelGroupInteractions', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[TravelGroupInteractions]
    (
        [GroupID]    int      NOT NULL,
        [MemberID]   int      NOT NULL,
        [ActionType] tinyint  NOT NULL,
        [CreatedAt]  datetime NOT NULL,

        CONSTRAINT [PK_TravelGroupInteractions]
            PRIMARY KEY CLUSTERED ([GroupID] ASC, [MemberID] ASC, [ActionType] ASC)
    );
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.default_constraints
    WHERE parent_object_id = OBJECT_ID(N'dbo.TravelGroupInteractions')
      AND name = N'DF_TravelGroupInteractions_CreatedAt'
)
BEGIN
    ALTER TABLE [dbo].[TravelGroupInteractions]
        ADD CONSTRAINT [DF_TravelGroupInteractions_CreatedAt]
        DEFAULT (GETDATE()) FOR [CreatedAt];
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_TravelGroupInteractions_Group'
)
BEGIN
    ALTER TABLE [dbo].[TravelGroupInteractions] WITH CHECK
        ADD CONSTRAINT [FK_TravelGroupInteractions_Group]
        FOREIGN KEY ([GroupID]) REFERENCES [dbo].[TravelGroups] ([GroupID])
        ON DELETE CASCADE;
END;

IF NOT EXISTS
(
    SELECT 1
    FROM sys.foreign_keys
    WHERE name = N'FK_TravelGroupInteractions_Member'
)
BEGIN
    ALTER TABLE [dbo].[TravelGroupInteractions] WITH CHECK
        ADD CONSTRAINT [FK_TravelGroupInteractions_Member]
        FOREIGN KEY ([MemberID]) REFERENCES [dbo].[Members] ([Id]);
END;

COMMIT TRANSACTION;
GO
