IF OBJECT_ID(N'dbo.VlogPostRoomExports', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.VlogPostRoomExports (
        PostId int NOT NULL CONSTRAINT PK_VlogPostRoomExports PRIMARY KEY,
        GroupId int NOT NULL,
        Country nvarchar(100) NOT NULL,
        Region nvarchar(200) NOT NULL,
        People int NOT NULL,
        ExportedAt datetime2 NOT NULL,
        CONSTRAINT FK_VlogPostRoomExports_Post FOREIGN KEY(PostId) REFERENCES dbo.VlogPosts(PostID),
        CONSTRAINT FK_VlogPostRoomExports_Group FOREIGN KEY(GroupId) REFERENCES dbo.TravelGroups(GroupID)
    );
    CREATE UNIQUE INDEX IX_VlogPostRoomExports_GroupId ON dbo.VlogPostRoomExports(GroupId);
END;